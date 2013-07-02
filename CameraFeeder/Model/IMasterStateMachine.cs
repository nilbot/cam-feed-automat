using System;
using System.Collections.Generic;

namespace Feeder.Model
{
    public interface IMasterStateMachine
    {
        event EventHandler UniversalStart;
        event EventHandler UniversalStop;

        Dictionary<Guid, string> NameDictionary { get; }
    }
}
