using System;

namespace Feeder.Model
{
    public class StateMachineTraverse
    {
        public readonly StateMachineEnum Origin;

        public readonly StateMachineEnum Next;

        private readonly Action _method;

        public StateMachineTraverse(StateMachineEnum origin, StateMachineEnum target, Action act)
        {
            Origin = origin;
            Next = target;
            _method = act;
        }

        public void Execute()
        {
            _method.Invoke();
        }
    }
}