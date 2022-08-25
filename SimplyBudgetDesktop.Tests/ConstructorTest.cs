﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;

namespace SimplyBudgetDesktop.Tests;

public interface IConstructorTest
{
    void Run();
}

public static class ConstructorTest
{
    private class Test : IConstructorTest
    {
        public Dictionary<Type, object?> SpecifiedValues { get; }
            = new Dictionary<Type, object?>();

        public List<Type> AllowedNullParameters { get; }
            = new List<Type>();

        private Type TargetType { get; }

        public Test(Type targetType)
        {
            TargetType = targetType;
        }

        public Test(Test original)
        {
            TargetType = original.TargetType;
            AllowedNullParameters.AddRange(original.AllowedNullParameters);
            foreach (KeyValuePair<Type, object?> kvp in original.SpecifiedValues)
            {
                SpecifiedValues[kvp.Key] = kvp.Value;
            }
        }

        public void Run()
        {
            foreach (ConstructorInfo constructor in TargetType.GetConstructors())
            {
                ParameterInfo[] parameters = constructor.GetParameters();

                object?[] parameterValues = parameters
                    .Select(p => p.ParameterType)
                    .Select(t =>
                    {
                        if (SpecifiedValues.TryGetValue(t, out object? value))
                        {
                            return value;
                        }
                        Mock mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(t))!;
                        return mock.Object;
                    })
                    .ToArray();

                for (int i = 0; i < parameters.Length; i++)
                {
                    object?[] values = parameterValues.ToArray();
                    values[i] = null;

                    if (AllowedNullParameters.Contains(parameters[i].ParameterType) ||
                        parameters[i].HasDefaultValue && parameters[i].DefaultValue is null)
                    {
                        //NB: no exception thrown
                        constructor.Invoke(values);
                    }
                    else
                    {
                        string parameterDisplay = $"'{parameters[i].Name}' ({parameters[i].ParameterType.Name})";
                        TargetInvocationException ex = Assert.ThrowsException<TargetInvocationException>(new Action(() =>
                        {
                            object? rv = constructor.Invoke(values);
                            throw new Exception($"Expected {nameof(ArgumentNullException)} for null parameter {parameterDisplay} but no exception was thrown");
                        }));
                        if (ex.InnerException is ArgumentNullException argumentNullException)
                        {
                            Assert.AreEqual(parameters[i].Name, argumentNullException.ParamName);
                        }
                        else
                        {
                            throw new Exception($"Thrown argument for {parameterDisplay} was '{ex.InnerException?.GetType().Name}' not {nameof(ArgumentNullException)}.", ex.InnerException);
                        }
                    }
                }
            }
        }
    }

    //NB: The necessity of this method is a code smell
    public static IConstructorTest AllowNull<TParameterType>(this IConstructorTest test)
    {
        if (test is Test internalTest)
        {
            var newTest = new Test(internalTest);
            newTest.AllowedNullParameters.Add(typeof(TParameterType));
            return newTest;
        }
        else
        {
            throw new ArgumentException("Argument not expected type", nameof(test));
        }
    }

    public static IConstructorTest Use<T>(this IConstructorTest test, T value)
    {
        if (test is Test internalTest)
        {
            var newTest = new Test(internalTest);
            newTest.SpecifiedValues.Add(typeof(T), value);
            return newTest;
        }
        else
        {
            throw new ArgumentException("Argument not expected type", nameof(test));
        }
    }

    public static IConstructorTest BuildArgumentNullExceptionsTest<T>()
        => new Test(typeof(T));

    public static void AssertArgumentNullExceptions<T>()
        => BuildArgumentNullExceptionsTest<T>().Run();
}