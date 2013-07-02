using System;

namespace Feeder.Common.Attributes
{
    /// <summary>
    ///     to describe tbd state and contains notes
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class ToBeDefinedAttribute : Attribute
    {
        public ToBeDefinedAttribute(params string[] notes)
        {
            Notes = notes;
        }

        public string[] Notes { get; set; }
    }
}
