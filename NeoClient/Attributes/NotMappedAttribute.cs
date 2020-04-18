using System;

namespace NeoClient.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NotMappedAttribute : Attribute
    {
    }
}
