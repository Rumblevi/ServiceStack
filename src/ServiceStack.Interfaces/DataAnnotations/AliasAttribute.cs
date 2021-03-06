using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class AliasAttribute : AttributeBase
    {
        public string Name { get; set; }

        public AliasAttribute(string name)
        {
            this.Name = name;
        }
    }
}