﻿#region License
//
// Copyright (c) 2008-2014, Dolittle (http://www.dolittle.com)
//
// Licensed under the MIT License (http://opensource.org/licenses/MIT)
//
// You may not use this file except in compliance with the License.
// You may obtain a copy of the license at 
//
//   http://github.com/dolittle/Bifrost/blob/master/MIT-LICENSE.txt
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bifrost.Concepts;
using Bifrost.Execution;
using Bifrost.Extensions;
using Bifrost.Validation.MetaData;
using FluentValidation;
using FluentValidation.Validators;

namespace Bifrost.FluentValidation.MetaData
{
    /// <summary>
    /// Represents an implementation of <see cref="IValidationMetaDataGenerator"/>
    /// </summary>
    public class ValidationMetaDataGenerator : IValidationMetaDataGenerator
    {
        ITypeDiscoverer _typeDiscoverer;
        IContainer _container;

        Dictionary<Type, ICanGenerateRule> _generatorsByType = new Dictionary<Type, ICanGenerateRule>();
        Dictionary<Type, Type> _inputValidatorsByType = new Dictionary<Type, Type>();


        /// <summary>
        /// Initializes a new instance of <see cref="ValidationMetaDataGenerator"/>
        /// </summary>
        /// <param name="typeDiscoverer"><see cref="ITypeDiscoverer"/> to use for discovering generators</param>
        /// <param name="container"><see cref="IContainer"/> to use for activation of generators</param>
        public ValidationMetaDataGenerator(ITypeDiscoverer typeDiscoverer, IContainer container)
        {
            _typeDiscoverer = typeDiscoverer;
            _container = container;

            _inputValidatorsByType = typeDiscoverer.FindMultiple(typeof(IValidateInput<>))
                            .Where(t => typeof(IValidator).IsAssignableFrom(t))
                            .ToDictionary<Type,Type>(t=>t.GetGenericArguments()[0]);

            PopulateGenerators();
        }


#pragma warning disable 1591 // Xml Comments

        public ValidationMetaData GenerateFor(Type typeForValidation)
        {
            var metaData = new ValidationMetaData();

            if (_inputValidatorsByType.ContainsKey(typeForValidation))
            {
                var validator = _container.Get(_inputValidatorsByType[typeForValidation]) as IValidator;
                GetValue(validator, metaData, string.Empty);
            }

            return metaData;
        }


        void GetValue(IValidator inputValidator, ValidationMetaData metaData, string parentKey, bool isParentConcept = false, bool isParentModelRule = false)
        {
            var inputValidatorType = inputValidator.GetType();
            var genericArguments = inputValidatorType.BaseType.GetGenericArguments();

            var descriptor = inputValidator.CreateDescriptor();
            var members = descriptor.GetMembersWithValidators();
            
            foreach (var member in members)
            {
                var rules = descriptor.GetRulesForMember(member.Key);
                foreach (var rule in rules)
                {
                    foreach (var validator in rule.Validators)
                    {
                        var isModelRule = member.Key == ModelRule<string>.ModelRulePropertyName;
                        var currentKey = string.Empty;
                        if (isParentConcept || isParentModelRule || isModelRule)
                            currentKey = parentKey;
                        else
                            currentKey = string.IsNullOrEmpty(parentKey) ? member.Key : string.Format("{0}.{1}", parentKey, member.Key.ToCamelCase());

                        if (validator is ChildValidatorAdaptor)
                        {
                            var isConcept = false;
                            
                            if (genericArguments.Length == 1)
                            {
                                var type = isModelRule ? genericArguments[0] : GetPropertyInfo(genericArguments[0], member.Key).PropertyType;
                                isConcept = type.IsConcept();
                            }

                            var childValidator = (validator as ChildValidatorAdaptor).Validator;
                            GetValue(childValidator, metaData, currentKey, isConcept, isModelRule);
                        }
                        else if (validator is IPropertyValidator)
                        {
                            GenerateFor(metaData, currentKey, validator as IPropertyValidator);
                        }
                    }
                }
            }
        }
#pragma warning restore 1591 // Xml Comments

        void GenerateFor(ValidationMetaData metaData, string property, IPropertyValidator validator)
        {
            var validatorType = validator.GetType();
            var types = new List<Type>();
            types.Add(validatorType);
            types.AddRange(validatorType.GetInterfaces());
            foreach (var type in types)
            {
                if (_generatorsByType.ContainsKey(type))
                {
                    var propertyName = property.ToCamelCase();
                    var rule = _generatorsByType[type].GeneratorFrom(property, validator);
                    var ruleName = rule.GetType().Name.ToCamelCase();
                    metaData[propertyName][ruleName] = rule;
                }
            }
        }

        void PopulateGenerators()
        {
            var generatorTypes = _typeDiscoverer.FindMultiple<ICanGenerateRule>();
            foreach (var generatorType in generatorTypes)
            {
                var generator = _container.Get(generatorType) as ICanGenerateRule;
                foreach (var validatorType in generator.From)
                {
                    _generatorsByType[validatorType] = generator;
                }
            }
        }

        PropertyInfo GetPropertyInfo(Type type, string name)
        {
            return type.GetProperty(name);
        }
    }
}