﻿using System.Reflection;
using Bifrost.Mapping;

namespace Bifrost.Specs.Mapping.for_MappingTargets
{
    public class StringMappingTarget : MappingTargetFor<string>
    {
        protected override void SetValue(string target, MemberInfo member, object value)
        {
            
        }
    }
}
