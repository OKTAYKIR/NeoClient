using System;
using System.Collections.Generic;
using System.Text;

namespace NeoClient.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RelationshipAttribute : Attribute
    {
        public DIRECTION Direction { get; set; } = DIRECTION.INCOMING;
        public string Name { get; set; }
    }
}
