using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace ClothoSharedItems
{
    /// <summary>
    /// Exception thrown to indicate that an inappropriate type argument was used for
    /// a type parameter to a generic type or method.
    /// </summary>
    public class TypeArgumentException : Exception
    {
        /// <summary>
        /// Constructs a new instance of TypeArgumentException with no message.
        /// </summary>
        public TypeArgumentException()
        {
        }

        /// <summary>
        /// Constructs a new instance of TypeArgumentException with the given message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public TypeArgumentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a new instance of TypeArgumentException with the given message and inner exception.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">Inner exception.</param>
        public TypeArgumentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Constructor provided for serialization purposes.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Context</param>
        protected TypeArgumentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Provides a set of static methods for use with enum types. Much of
    /// what's available here is already in System.Enum, but this class
    /// provides a strongly typed API.
    /// </summary>
    internal static class Enums
    {
        /// <summary>
        /// Returns an array of values in the enum.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>An array of values in the enum</returns>
        public static T[] GetValuesArray<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        /// <summary>
        /// Returns the values for the given enum as an immutable list.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        public static IList<T> GetValues<T>() where T : struct
        {
            return EnumInternals<T>.Values;
        }

        /// <summary>
        /// Returns an array of names in the enum.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>An array of names in the enum</returns>
        public static string[] GetNamesArray<T>() where T : struct
        {
            return Enum.GetNames(typeof(T));
        }

        /// <summary>
        /// Returns the names for the given enum as an immutable list.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>An array of names in the enum</returns>
        public static IList<string> GetNames<T>() where T : struct
        {
            return EnumInternals<T>.Names;
        }

        /// <summary>
        /// Checks whether the value is a named value for the type.
        /// </summary>
        /// <remarks>
        /// For flags enums, it is possible for a value to be a valid
        /// combination of other values without being a named value
        /// in itself. To test for this possibility, use IsValidCombination.
        /// </remarks>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Value to test</param>
        /// <returns>True if this value has a name, False otherwise.</returns>
        public static bool IsNamedValue<T>(this T value) where T : struct
        {
            // TODO: Speed this up for big enums
            return GetValues<T>().Contains(value);
        }

        /// <summary>
        /// Returns the description for the given value,
        /// as specified by DescriptionAttribute, or null
        /// if no description is present.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="item">Value to fetch description for</param>
        /// <returns>The description of the value, or null if no description
        /// has been specified (but the value is a named value).</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="item"/>
        /// is not a named member of the enum</exception>
        public static string GetDescription<T>(this T item) where T : struct
        {
            string description;
            if (EnumInternals<T>.ValueToDescriptionMap.TryGetValue(item, out description))
            {
                return description;
            }
            throw new ArgumentOutOfRangeException("item");
        }

        /// <summary>
        /// Attempts to find a value with the given description.
        /// </summary>
        /// <remarks>
        /// More than one value may have the same description. In this unlikely
        /// situation, the first value with the specified description is returned.
        /// </remarks>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="description">Description to find</param>
        /// <param name="value">Enum value corresponding to given description (on return)</param>
        /// <returns>True if a value with the given description was found,
        /// false otherwise.</returns>
        public static bool TryParseDescription<T>(string description, out T value)
            where T : struct
        {
            return EnumInternals<T>.DescriptionToValueMap.TryGetValue(description, out value);
        }

        /// <summary>
        /// Parses the name of an enum value.
        /// </summary>
        /// <remarks>
        /// This method only considers named values: it does not parse comma-separated
        /// combinations of flags enums.
        /// </remarks>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>The parsed value</returns>
        /// <exception cref="ArgumentException">The name could not be parsed.</exception>
        public static T ParseName<T>(string name) where T : struct
        {
            T value;
            if (!TryParseName(name, out value))
            {
                throw new ArgumentException("Unknown name", "name");
            }
            return value;
        }

        /// <summary>
        /// Attempts to find a value for the specified name.
        /// Only names are considered - not numeric values.
        /// </summary>
        /// <remarks>
        /// If the name is not parsed, <paramref name="value"/> will
        /// be set to the zero value of the enum. This method only
        /// considers named values: it does not parse comma-separated
        /// combinations of flags enums.
        /// </remarks>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="name">Name to parse</param>
        /// <param name="value">Enum value corresponding to given name (on return)</param>
        /// <returns>Whether the parse attempt was successful or not</returns>
        public static bool TryParseName<T>(string name, out T value) where T : struct
        {
            // TODO: Speed this up for big enums
            int index = EnumInternals<T>.Names.IndexOf(name);
            if (index == -1)
            {
                value = default(T);
                return false;
            }
            value = EnumInternals<T>.Values[index];
            return true;
        }

        /// <summary>
        /// Returns the underlying type for the enum
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>The underlying type (Byte, Int32 etc) for the enum</returns>
        public static Type GetUnderlyingType<T>() where T : struct
        {
            return EnumInternals<T>.UnderlyingType;
        }
    }

    /// <summary>
    /// Shared constants used by Flags and Enums.
    /// </summary>
    internal static class EnumInternals<T> where T : struct
    {
        internal static readonly bool IsFlags;
        internal static readonly Func<T, T, T> Or;
        internal static readonly Func<T, T, T> And;
        internal static readonly Func<T, T> Not;
        internal static readonly T UsedBits;
        internal static readonly T AllBits;
        internal static readonly T UnusedBits;
        internal static Func<T, T, bool> Equality;
        internal static readonly Func<T, bool> IsEmpty;
        internal static readonly IList<T> Values;
        internal static readonly IList<string> Names;
        internal static readonly Type UnderlyingType;
        internal static readonly Dictionary<T, string> ValueToDescriptionMap;
        internal static readonly Dictionary<string, T> DescriptionToValueMap;

        static EnumInternals()
        {
            Values = new ReadOnlyCollection<T>((T[])Enum.GetValues(typeof(T)));
            Names = new ReadOnlyCollection<string>(Enum.GetNames(typeof(T)));
            ValueToDescriptionMap = new Dictionary<T, string>();
            DescriptionToValueMap = new Dictionary<string, T>();
            foreach (T value in Values)
            {
                string description = GetDescription(value);
                ValueToDescriptionMap[value] = description;
                if (description != null && !DescriptionToValueMap.ContainsKey(description))
                {
                    DescriptionToValueMap[description] = value;
                }
            }
            UnderlyingType = Enum.GetUnderlyingType(typeof(T));
            IsFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
            // Parameters for various expression trees
            ParameterExpression param1 = Expression.Parameter(typeof(T), "x");
            ParameterExpression param2 = Expression.Parameter(typeof(T), "y");
            Expression convertedParam1 = Expression.Convert(param1, UnderlyingType);
            Expression convertedParam2 = Expression.Convert(param2, UnderlyingType);
            Equality = Expression.Lambda<Func<T, T, bool>>(Expression.Equal(convertedParam1, convertedParam2), param1, param2).Compile();
            Or = Expression.Lambda<Func<T, T, T>>(Expression.Convert(Expression.Or(convertedParam1, convertedParam2), typeof(T)), param1, param2).Compile();
            And = Expression.Lambda<Func<T, T, T>>(Expression.Convert(Expression.And(convertedParam1, convertedParam2), typeof(T)), param1, param2).Compile();
            Not = Expression.Lambda<Func<T, T>>(Expression.Convert(Expression.Not(convertedParam1), typeof(T)), param1).Compile();
            IsEmpty = Expression.Lambda<Func<T, bool>>(Expression.Equal(convertedParam1,
                Expression.Constant(Activator.CreateInstance(UnderlyingType))), param1).Compile();

            UsedBits = default(T);
            foreach (T value in Enums.GetValues<T>())
            {
                UsedBits = Or(UsedBits, value);
            }
            AllBits = Not(default(T));
            UnusedBits = And(AllBits, (Not(UsedBits)));
        }

        private static string GetDescription(T value)
        {
            FieldInfo field = typeof(T).GetField(value.ToString());
            return field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .Cast<DescriptionAttribute>()
                        .Select(x => x.Description)
                        .FirstOrDefault();
        }
    }

    /// <summary>
    /// Provides a set of static methods for use with "flags" enums,
    /// i.e. those decorated with <see cref="FlagsAttribute"/>.
    /// Other than <see cref="IsValidCombination{T}"/>, methods in this
    /// class throw <see cref="TypeArgumentException" />.
    /// </summary>
    public static class Flags
    {
        /// <summary>
        /// Helper method used by almost all methods to make sure
        /// the type argument is really a flags enum.
        /// </summary>
        private static void ThrowIfNotFlags<T>() where T : struct
        {
            if (!EnumInternals<T>.IsFlags)
            {
                throw new TypeArgumentException("Can't call this method for a non-flags enum");
            }
        }

        /// <summary>
        /// Returns whether or not the specified enum is a "flags" enum,
        /// i.e. whether it has FlagsAttribute applied to it.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>True if the enum type is decorated with
        /// FlagsAttribute; False otherwise.</returns>
        public static bool IsFlags<T>() where T : struct
        {
            return EnumInternals<T>.IsFlags;
        }

        /// <summary>
        /// Determines whether the given value only uses bits covered
        /// by named values.
        /// </summary>
        /// internal static
        /// <param name="values">Combination to test</param>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static bool IsValidCombination<T>(this T values) where T : struct
        {
            ThrowIfNotFlags<T>();
            return values.And(EnumInternals<T>.UnusedBits).IsEmpty();
        }

        /// <summary>
        /// Determines whether the two specified values have any flags in common.
        /// </summary>
        /// <param name="value">Value to test</param>
        /// <param name="desiredFlags">Flags we wish to find</param>
        /// <returns>Whether the two specified values have any flags in common.</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static bool HasAny<T>(this T value, T desiredFlags) where T : struct
        {
            ThrowIfNotFlags<T>();
            return value.And(desiredFlags).IsNotEmpty();
        }

        /// <summary>
        /// Determines whether all of the flags in <paramref name="desiredFlags"/>
        /// </summary>
        /// <param name="value">Value to test</param>
        /// <param name="desiredFlags">Flags we wish to find</param>
        /// <returns>Whether all the flags in <paramref name="desiredFlags"/> are in <paramref name="value"/>.</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static bool HasAll<T>(this T value, T desiredFlags) where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.Equality(value.And(desiredFlags), desiredFlags);
        }

        /// <summary>
        /// Returns the bitwise "and" of two values.
        /// </summary>
        /// internal static
        /// <param name="first">First value</param>
        /// <param name="second">Second value</param>
        /// <returns>The bitwise "and" of the two values</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static T And<T>(this T first, T second) where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.And(first, second);
        }

        /// <summary>
        /// Returns the bitwise "or" of two values.
        /// </summary>
        /// internal static
        /// <param name="first">First value</param>
        /// <param name="second">Second value</param>
        /// <returns>The bitwise "or" of the two values</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static T Or<T>(this T first, T second) where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.Or(first, second);
        }

        /// <summary>
        /// Returns all the bits used in any flag values
        /// </summary>
        /// internal static
        /// <returns>A flag value with all the bits set that are ever set in any defined value</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static T GetUsedBits<T>() where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.UsedBits;
        }

        /// <summary>
        /// Returns the inverse of a value, with no consideration for which bits are used
        /// by values within the enum (i.e. a simple bitwise negation).
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Value to invert</param>
        /// <returns>The bitwise negation of the value</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static T AllBitsInverse<T>(this T value) where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.Not(value);
        }

        /// <summary>
        /// Returns the inverse of a value, but limited to those bits which are used by
        /// values within the enum.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Value to invert</param>
        /// <returns>The restricted inverse of the value</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static T UsedBitsInverse<T>(this T value) where T : struct
        {
            ThrowIfNotFlags<T>();
            return value.AllBitsInverse().And(EnumInternals<T>.UsedBits);
        }

        /// <summary>
        /// Returns whether this value is an empty set of fields, i.e. the zero value.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Value to test</param>
        /// <returns>True if the value is empty (zero); False otherwise.</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static bool IsEmpty<T>(this T value) where T : struct
        {
            ThrowIfNotFlags<T>();
            return EnumInternals<T>.IsEmpty(value);
        }

        /// <summary>
        /// Returns whether this value has any fields set, i.e. is not zero.
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Value to test</param>
        /// <returns>True if the value is non-empty (not zero); False otherwise.</returns>
        /// <exception cref="TypeArgumentException"><typeparamref name="T"/> is not a flags enum.</exception>
        public static bool IsNotEmpty<T>(this T value) where T : struct
        {
            ThrowIfNotFlags<T>();
            return !value.IsEmpty();
        }
    }
}