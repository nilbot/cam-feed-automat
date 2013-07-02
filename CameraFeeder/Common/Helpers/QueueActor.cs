using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Feeder.Common.Helpers
{
    public sealed class QueueActor<T>
    {
        private readonly   ActorSynchronizationContext _messageQueue = new ActorSynchronizationContext();
        private readonly   Queue<T>  _items = new Queue<T>();

        public async Task EnqueueAsync(T item)
        {
            await _messageQueue;
            _items.Enqueue(item);
        }

        public async Task<Probable<T>> TryDequeueAsync()
        {
            await _messageQueue;
            if (_items.Count == 0) return Probable.NoValue;
            return _items.Dequeue();
        }
    }

    public sealed class ActorSynchronizationContext : SynchronizationContext
    {
        private readonly SynchronizationContext _subContext;
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private int _pendingCount;

        public ActorSynchronizationContext(SynchronizationContext subContext = null)
        {
            _subContext = subContext ?? new SynchronizationContext();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException("d");
            _pending.Enqueue(() => d(state));

            // trigger consumption when the queue was empty
            if (Interlocked.Increment(ref _pendingCount) == 1)
                _subContext.Post(consume, null);
        }
        private void consume(object state)
        {
            var _surroundingContext = Current;
            try
            {
                // temporarily replace surrounding sync context with this context
                SetSynchronizationContext(this);

                // run pending actions until there are no more
                do
                {
                    Action _a;
                    _pending.TryDequeue(out _a); // always succeeds, due to usage of _pendingCount
                    _a.Invoke(); // if an enqueued action throws... well, that's very bad
                } while (Interlocked.Decrement(ref _pendingCount) > 0);

            }
            finally
            {
                SetSynchronizationContext(_surroundingContext); // restore surrounding sync context
            }
        }

        //public override void Send(SendOrPostCallback d, object state)
        //{
        //    throw new NotSupportedException();
        //}
        //public override SynchronizationContext CreateCopy()
        //{
        //    return this;
        //}
    }

    public sealed class SynchronizationContextAwaiter : INotifyCompletion
    {
        private readonly SynchronizationContext _context;
        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context;
        }
        public bool IsCompleted
        {
            get
            {
                // always re-enter, even if already in the context
                return false;
            }
        }
        public void OnCompleted(Action action)
        {
            // resume inside the context
            _context.Post(x => action(), null);
        }
        public void GetResult()
        {
            // no value to return, no exceptions to propagate
        }
    }
    public static class SynchronizationContextExtensions
    {
        public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            return new SynchronizationContextAwaiter(context);
        }
    }

    ///<summary>
    ///A potential value that may contain no value or may contain a value of type T.
    ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
    ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
    ///</summary>
    [DebuggerDisplay("{ToString()}")]
    public struct Probable<T>: IProbablyHaveValue,IEquatable<Probable<T>>
    {
        ///<summary>
        ///A potential value containing no value.
        ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
        ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
        ///</summary>
        public static Probable<T> NoValue { get { return default(Probable<T>); } }

        private readonly T _value;
        private readonly bool _hasValue;

        ///<summary>Determines if this potential value contains a value or not.</summary>
        public bool HasValue { get { return _hasValue; } }
        
        ///<summary>Constructs a potential value containing the given value.</summary>
        public Probable(T value)
        {
            _hasValue = true;
            _value = value;
        }

        ///<summary>Matches this potential value into either a function expecting a value or a function expecting no value, returning the result.</summary>
        public TOut Match<TOut>(Func<T, TOut> valueProjection, Func<TOut> alternativeFunc)
        {
            if(valueProjection==null)throw new ArgumentNullException("valueProjection");
            if(alternativeFunc==null)throw new ArgumentNullException("alternativeFunc");
            return _hasValue ? valueProjection(_value) : alternativeFunc();
        }

        ///<summary>Returns a potential value containing no value.</summary>
        public static implicit operator Probable<T>(ProbablyNoValue noValue)
        {
            return NoValue;
        }
        ///<summary>Returns a potential value containing the given value.</summary>
        public static implicit operator Probable<T>(T value)
        {
            return new Probable<T>(value);
        }
        ///<summary>Returns the value contained in the potential value, throwing a cast exception if the potential value contains no value.</summary>
        public static explicit operator T(Probable<T> potentialValue)
        {
            if(!potentialValue._hasValue) throw new InvalidCastException("No Value");
            return potentialValue._value;
        }

        ///<summary>Determines if two potential values are equivalent.</summary>
        public static bool operator ==(Probable<T> potentialValue1, Probable<T> potentialValue2)
        {
            return potentialValue1.Equals(potentialValue2);
        }
        ///<summary>Determines if two potential values are not equivalent.</summary>
        public static bool operator !=(Probable<T> potentialValue1, Probable<T> potentialValue2)
        {
            return !potentialValue1.Equals(potentialValue2);
        }

        ///<summary>Determines if two potential values are equivalent.</summary>
        public static bool operator ==(Probable<T> potentialValue1, IProbablyHaveValue potentialValue2)
        {
            return potentialValue1.Equals(potentialValue2);
        }
        ///<summary>Determines if two potential values are not equivalent.</summary>
        public static bool operator !=(Probable<T> potentialValue1, IProbablyHaveValue potentialValue2)
        {
            return !potentialValue1.Equals(potentialValue2);
        }

        ///<summary>Returns the hash code for this potential value.</summary>
        public override int GetHashCode()
        {
            return !_hasValue ? 0
                 : ReferenceEquals(_value, null) ? -1
                 : _value.GetHashCode();
        }
        ///<summary>
        ///Determines if this potential value is equivalent to the given potential value.
        ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
        ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
        ///</summary>
        public bool Equals(Probable<T> other)
        {
            if (other._hasValue != _hasValue) return false;
            return !_hasValue || Equals(_value, other._value);
        }
        ///<summary>
        ///Determines if this potential value is equivalent to the given potential value.
        ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
        ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
        ///</summary>
        public bool Equals(IProbablyHaveValue other)
        {
            if (other is Probable<T>) return Equals((Probable<T>)other);
            // potential values containing no value are always equal
            return other != null && !HasValue && !other.HasValue;
        }
        ///<summary>
        ///Determines if this potential value is equivalent to the given object.
        ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
        ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
        ///</summary>
        public override bool Equals(object obj)
        {
            if (obj is Probable<T>) return Equals((Probable<T>)obj);
            return Equals(obj as IProbablyHaveValue);
        }
        ///<summary>Returns a string representation of this potential value.</summary>
        public override string ToString()
        {
            return _hasValue
                 ? String.Format("Value: {0}", _value)
                 : "No Value";
        }

    }
    ///<summary>
    ///A potential value that may or may not contain an unknown value of unknown type.
    ///All implementations should compare equal and have a hash code of 0 when HasValue is false.
    ///</summary>
    ///<remarks>
    ///Used to allow comparisons of the raw Probable.NoValue to generic ones like Probable&lt;int&gt;.NoValue.
    ///Also used as the result type of the 'do action if value present' method, but only because there is no standard void or unit type.
    ///</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProbablyHaveValue : IEquatable<IProbablyHaveValue>
    {
        ///<summary>Determines if this potential value contains a value or not.</summary>
        bool HasValue { get; }
    }

    ///<summary>
    ///A non-generic lack-of-value type, equivalent to generic likes like lack-of-int.
    ///Use Strilanc.Value.Probable.NoValue to get an instance.
    ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
    ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
    ///</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerDisplay("{ToString()}")]
    public struct ProbablyNoValue : IProbablyHaveValue
    {
        ///<summary>Determines if this potential value contains a value or not (it doesn't).</summary>
        public bool HasValue { get { return false; } }
        ///<summary>Returns the hash code for a lack of potential value.</summary>
        public override int GetHashCode()
        {
            return 0;
        }
        ///<summary>Determines if the given potential value contains no value.</summary>
        public bool Equals(IProbablyHaveValue other)
        {
            return other != null && !other.HasValue;
        }
        ///<summary>Determines if the given object is a potential value containing no value.</summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as IProbablyHaveValue);
        }
        ///<summary>Determines if two lack of values are equal (they are).</summary>
        public static bool operator ==(ProbablyNoValue noValue1, ProbablyNoValue noValue2)
        {
            return true;
        }
        ///<summary>Determines if two lack of values are not equal (they're not).</summary>
        public static bool operator !=(ProbablyNoValue noValue1, ProbablyNoValue noValue2)
        {
            return false;
        }
        ///<summary>Returns a string representation of this lack of value.</summary>
        public override string ToString()
        {
            return "No Value";
        }
    }

    ///<summary>Utility methods that involve Probable&lt;T&gt; but with a focus on other types.</summary>
    public static class ProbableUtilities
    {
        ///<summary>Returns the value contained in the given potential value as a nullable type, returning null if there is no contained value.</summary>
        public static T? AsNullable<T>(this Probable<T> potentialValue) where T : struct
        {
            return potentialValue.Select(e => (T?)e).ElseDefault();
        }

        ///<summary>Returns the value contained in the given nullable value as a potential value, with null corresponding to no value.</summary>
        public static Probable<T> AsMay<T>(this T? potentialValue) where T : struct
        {
            if (!potentialValue.HasValue) return Probable.NoValue;
            return potentialValue.Value;
        }

        /// <summary>
        /// Returns the result of using a folder function to combine all the items in the sequence into one aggregate item.
        /// If the sequence is empty, the result is NoValue.
        /// </summary>
        public static Probable<T> MayAggregate<T>(this IEnumerable<T> sequence, Func<T, T, T> folder)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (folder == null) throw new ArgumentNullException("folder");
            return sequence.Aggregate(
                Probable<T>.NoValue,
                (a, e) => a.Match(v => folder(v, e), e));
        }

        /// <summary>
        /// Returns the minimum value in a sequence, as determined by the given comparer or else the type's default comparer.
        /// If the sequence is empty, the result is NoValue.
        /// </summary>
        public static Probable<T> MayMin<T>(this IEnumerable<T> sequence, IComparer<T> comparer = null)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var _c = comparer ?? Comparer<T>.Default;
            return sequence.MayAggregate((e1, e2) => _c.Compare(e1, e2) <= 0 ? e1 : e2);
        }

        /// <summary>
        /// Returns the maximum value in a sequence, as determined by the given comparer or else the type's default comparer.
        /// If the sequence is empty, the result is NoValue.
        /// </summary>
        public static Probable<T> MayMax<T>(this IEnumerable<T> sequence, IComparer<T> comparer = null)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var _c = comparer ?? Comparer<T>.Default;
            return sequence.MayAggregate((e1, e2) => _c.Compare(e1, e2) >= 0 ? e1 : e2);
        }

        /// <summary>
        /// Returns the minimum value in a sequence, as determined by projecting the items and using the given comparer or else the type's default comparer.
        /// If the sequence is empty, the result is NoValue.
        /// </summary>
        public static Probable<TItem> MayMinBy<TItem, TCompare>(this IEnumerable<TItem> sequence, Func<TItem, TCompare> projection, IComparer<TCompare> comparer = null)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var _c = comparer ?? Comparer<TCompare>.Default;
            return sequence
                .Select(e => new { v = e, p = projection(e) })
                .MayAggregate((e1, e2) => _c.Compare(e1.p, e2.p) <= 0 ? e1 : e2)
                .Select(e => e.v);
        }

        /// <summary>
        /// Returns the maximum value in a sequence, as determined by projecting the items and using the given comparer or else the type's default comparer.
        /// If the sequence is empty, the result is NoValue.
        /// </summary>
        public static Probable<TItem> MayMaxBy<TItem, TCompare>(this IEnumerable<TItem> sequence, Func<TItem, TCompare> projection, IComparer<TCompare> comparer = null)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var _c = comparer ?? Comparer<TCompare>.Default;
            return sequence
                .Select(e => new { v = e, p = projection(e) })
                .MayAggregate((e1, e2) => _c.Compare(e1.p, e2.p) >= 0 ? e1 : e2)
                .Select(e => e.v);
        }

        ///<summary>Returns the first item in a sequence, or else NoValue if the sequence is empty.</summary>
        public static Probable<T> MayFirst<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            using (var _e = sequence.GetEnumerator())
                if (_e.MoveNext())
                    return _e.Current;
            return Probable.NoValue;
        }

        ///<summary>Returns the last item in a sequence, or else NoValue if the sequence is empty.</summary>
        public static Probable<T> MayLast<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");

            // try to skip to the last item without enumerating when possible
            var _list = sequence as IList<T>;
            if (_list != null)
            {
                if (_list.Count == 0) return Probable.NoValue;
                return _list[_list.Count - 1];
            }

            return sequence.MayAggregate((e1, e2) => e2);
        }

        ///<summary>Returns the single item in a sequence, NoValue if the sequence is empty, or throws an exception if there is more than one item.</summary>
        public static Probable<T> MaySingle<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");

            using (var _e = sequence.GetEnumerator())
            {
                if (!_e.MoveNext()) return Probable.NoValue;
                var _result = _e.Current;
                //note: this case is an exception to match the semantics of SingleOrDefault, not because it's the best approach
                if (_e.MoveNext()) throw new ArgumentOutOfRangeException("sequence", @"Expected either no items or a single item.");
                return _result;
            }
        }

        /// <summary>
        /// Enumerates the values in the potential values in the sequence.
        /// The potential values that contain no value are skipped.
        /// </summary>
        public static IEnumerable<T> WhereHasValue<T>(this IEnumerable<Probable<T>> sequence)
        {
            return sequence.Where(e => e.HasValue).Select(e => (T)e);
        }

        /// <summary>
        /// Enumerates the values in all the potential values in the sequence.
        /// However, if any of the potential values contains no value then the entire result is no value.
        /// </summary>
        public static Probable<IEnumerable<T>> MayAll<T>(this IEnumerable<Probable<T>> sequence)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var _result = new List<T>();
            if (sequence.Any(potentialValue => !potentialValue.IfHasValueThenDo(_result.Add).HasValue))
            {
                return Probable.NoValue;
            }
            return _result;
        }
    }

    ///<summary>Utility methods for the generic Probable type.</summary>
    public static class Probable
    {
        ///<summary>
        ///A potential value containing no value. Implicitely converts to a no value of any generic Probable type.
        ///Note: All forms of no value are equal, including Probable.NoValue, Probable&lt;T&gt;.NoValue, Probable&lt;AnyOtherT&gt;.NoValue, default(Probable&lt;T&gt;) and new Probable&lt;T&gt;().
        ///Note: Null is NOT equivalent to new Probable&lt;object&gt;(null) and neither is equivalent to new Probable&lt;string&gt;(null).
        ///</summary>
        public static ProbablyNoValue NoValue { get { return default(ProbablyNoValue); } }

        ///<summary>Returns a potential value containing the given value.</summary>
        public static Probable<T> Maybe<T>(this T value)
        {
            return new Probable<T>(value);
        }
        ///<summary>Matches this potential value either into a function expecting a value or against an alternative value.</summary>
        public static TOut Match<TIn, TOut>(this Probable<TIn> potentialValue, Func<TIn, TOut> valueProjection, TOut alternative)
        {
            if (valueProjection == null) throw new ArgumentNullException("valueProjection");
            return potentialValue.Match(valueProjection, () => alternative);
        }
        ///<summary>Returns the potential result of potentially applying the given function to this potential value.</summary>
        public static Probable<TOut> Bind<TIn, TOut>(this Probable<TIn> potentialValue, Func<TIn, Probable<TOut>> projection)
        {
            if (projection == null) throw new ArgumentNullException("projection");
            return potentialValue.Match(projection, () => NoValue);
        }
        ///<summary>Returns the value contained in the given potential value, if any, or else the result of evaluating the given alternative value function.</summary>
        public static T Else<T>(this Probable<T> potentialValue, Func<T> alternativeFunc)
        {
            if (alternativeFunc == null) throw new ArgumentNullException("alternativeFunc");
            return potentialValue.Match(e => e, alternativeFunc);
        }
        ///<summary>Flattens a doubly-potential value, with the result containing a value only if both levels contained a value.</summary>
        public static Probable<T> Unwrap<T>(this Probable<Probable<T>> potentialValue)
        {
            return potentialValue.Bind(e => e);
        }
        ///<summary>Returns the value contained in the given potential value, if any, or else the result of evaluating the given alternative potential value function.</summary>
        public static Probable<T> Else<T>(this Probable<T> potentialValue, Func<Probable<T>> alternative)
        {
            if (alternative == null) throw new ArgumentNullException("alternative");
            return potentialValue.Match(e => e.Maybe(), alternative);
        }
        ///<summary>Returns the value contained in the given potential value, if any, or else the given alternative value.</summary>
        public static T Else<T>(this Probable<T> potentialValue, T alternative)
        {
            return potentialValue.Else(() => alternative);
        }
        ///<summary>Returns the value contained in the given potential value, if any, or else the given alternative potential value.</summary>
        public static Probable<T> Else<T>(this Probable<T> potentialValue, Probable<T> alternative)
        {
            return potentialValue.Else(() => alternative);
        }
        ///<summary>Returns the result of potentially applying a function to this potential value.</summary>
        public static Probable<TOut> Select<TIn, TOut>(this Probable<TIn> value, Func<TIn, TOut> projection)
        {
            if (projection == null) throw new ArgumentNullException("projection");
            return value.Bind(e => projection(e).Maybe());
        }
        ///<summary>Returns the same value, unless the contained value does not match the filter in which case a no value is returned.</summary>
        public static Probable<T> Where<T>(this Probable<T> value, Func<T, bool> filter)
        {
            if (filter == null) throw new ArgumentNullException("filter");
            return value.Bind(e => filter(e) ? e.Maybe() : NoValue);
        }
        ///<summary>Projects optional values, returning a no value if anything along the way is a no value.</summary>
        public static Probable<TOut> SelectMany<TIn, TMid, TOut>(this Probable<TIn> source,
                                                            Func<TIn, Probable<TMid>> maySelector,
                                                            Func<TIn, TMid, TOut> resultSelector)
        {
            if (maySelector == null) throw new ArgumentNullException("maySelector");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return source.Bind(s => maySelector(s).Select(m => resultSelector(s, m)));
        }
        ///<summary>Combines the values contained in several potential values with a projection function, returning no value if any of the inputs contain no value.</summary>
        public static Probable<TOut> Combine<TIn1, TIn2, TOut>(this Probable<TIn1> potentialValue1,
                                                          Probable<TIn2> potentialValue2,
                                                          Func<TIn1, TIn2, TOut> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return from v1 in potentialValue1
                   from v2 in potentialValue2
                   select resultSelector(v1, v2);
        }
        ///<summary>Combines the values contained in several potential values with a projection function, returning no value if any of the inputs contain no value.</summary>
        public static Probable<TOut> Combine<TIn1, TIn2, TIn3, TOut>(this Probable<TIn1> potentialValue1,
                                                                Probable<TIn2> potentialValue2,
                                                                Probable<TIn3> potentialValue3,
                                                                Func<TIn1, TIn2, TIn3, TOut> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return from v1 in potentialValue1
                   from v2 in potentialValue2
                   from v3 in potentialValue3
                   select resultSelector(v1, v2, v3);
        }
        ///<summary>Combines the values contained in several potential values with a projection function, returning no value if any of the inputs contain no value.</summary>
        public static Probable<TOut> Combine<TIn1, TIn2, TIn3, TIn4, TOut>(this Probable<TIn1> potentialValue1,
                                                                      Probable<TIn2> potentialValue2,
                                                                      Probable<TIn3> potentialValue3,
                                                                      Probable<TIn4> potentialValue4,
                                                                      Func<TIn1, TIn2, TIn3, TIn4, TOut> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return from v1 in potentialValue1
                   from v2 in potentialValue2
                   from v3 in potentialValue3
                   from v4 in potentialValue4
                   select resultSelector(v1, v2, v3, v4);
        }
        /// <summary>
        /// Potentially runs an action taking the potential value's value.
        /// No effect if the potential value is no value.
        /// Returns an IMayHaveValue that has a value iff the action was run.
        /// </summary>
        public static IProbablyHaveValue IfHasValueThenDo<T>(this Probable<T> potentialValue, Action<T> hasValueAction)
        {
            if (hasValueAction == null) throw new ArgumentNullException("hasValueAction");
            return potentialValue.Select(e =>
            {
                hasValueAction(e);
                return 0;
            });
        }
        ///<summary>Runs the given no value action if the given potential value does not contain a value, and otherwise does nothing.</summary>
        public static void ElseDo(this IProbablyHaveValue potentialValue, Action noValueAction)
        {
            if (potentialValue == null) throw new ArgumentNullException("potentialValue");
            if (noValueAction == null) throw new ArgumentNullException("noValueAction");
            if (!potentialValue.HasValue) noValueAction();
        }
        ///<summary>Returns the value contained in the given potential value, if any, or else the type's default value.</summary>
        public static T ElseDefault<T>(this Probable<T> potentialValue)
        {
            return potentialValue.Else(default(T));
        }

        ///<summary>Returns the value contained in the potential value, or throws an InvalidOperationException if it contains no value.</summary>
        public static T ForceGetValue<T>(this Probable<T> potentialValue)
        {
            return potentialValue.Match(
                e => e,
                () => { throw new InvalidOperationException("No Value"); });
        }
    }


}
