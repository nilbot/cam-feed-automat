using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;


namespace Feeder.Model
{
    public class RingBuffer
    {
        private readonly object _lock = new Object();
        private readonly Queue<Bitmap> _postSnapshot;
        private readonly Queue<Bitmap> _preSnapshot;
        private int _preringSize;
        private int _postringSize;

        //public static T DeepClone<T>(T obj)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        var formatter = new BinaryFormatter();
        //        formatter.Serialize(ms, obj);
        //        ms.Position = 0;

        //        return (T)formatter.Deserialize(ms);
        //    }
        //}

        public RingBuffer(int presize, int postsize)
        {
            _preringSize = presize;
            _postringSize = postsize;
            _preSnapshot = new Queue<Bitmap>(_preringSize);
            _postSnapshot = new Queue<Bitmap>(_postringSize);
            Triggered = false;
        }

        protected bool Triggered { get; private set; }

        public int Count
        {
            get { return _preSnapshot.Count+_postSnapshot.Count; }
        }

        public event EventHandler SnapshotReady;

        public void Enqueue(Bitmap t)
        {

            if (!Processing)
            {
                if (!Triggered)
                {
                    lock (_lock)
                    {
                        if (_preSnapshot.Count < _preringSize)
                            _preSnapshot.Enqueue(new Bitmap(t));

                        else
                        {
                            var _toThrowAway = _preSnapshot.Dequeue();
                            _toThrowAway.Dispose();
                            _preSnapshot.Enqueue(new Bitmap(t));
                        }
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        if (_postSnapshot.Count < _postringSize)
                            _postSnapshot.Enqueue(new Bitmap(t));
                        else
                        {

                            //_postSnapshot = new T[_queue.Count];
                            //_queue.CopyTo(_postSnapshot, 0);
                            //_postSnapshot = DeepClone(_queue);
                            Triggered = false;
                            Processing = true;
                            if (SnapshotReady != null)
                                SnapshotReady(this, null);
                        }
                    }
                }
            }
            }
        


        public void SetPreRingSize(int size)
        {
            lock (_lock)
            {
                _preringSize = size;
            }
        }

        public void SetPostRingSize(int size)
        {
            lock (_lock)
            {
                _postringSize = size;
            }
        }

        public void SavePreSnapshot(Bitmap t)
        {
            lock (_lock)
            {
                _preSnapshot.Enqueue(new Bitmap(t));
                //_preSnapshot = new T[_queue.Count];
                ////_preSnapshot = DeepClone(_queue);
                //_queue.CopyTo(_preSnapshot, 0);
                Triggered = true;
                //set flag to stop new frames into pre (and post)
            }
        }

        public bool Processing { get; set; }

        public void Clear()
        {
            // dequeue length of post from pre, append post to pre

            lock (_lock)
            {
                if (_postSnapshot.Count != 0)
                {
                    for (int _i = 0; _i != _postSnapshot.Count; _i++)
                    {
                        var _toThrowAway =  _preSnapshot.Dequeue();
                        _toThrowAway.Dispose();
                        _preSnapshot.Enqueue(_postSnapshot.Dequeue());
                    }
                }
            }
            
    
            // clear post
            lock (_lock) _postSnapshot.Clear();

            // set flag to enable new frames into pre
            Processing = false;
        }

        public Bitmap[] GrabSnapshot()
        {
            return  (_preSnapshot.Concat(_postSnapshot)).ToArray();
        }
    }
}
