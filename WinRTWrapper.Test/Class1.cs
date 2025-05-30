using System;
using WinRTWrapper.CodeAnalysis;

namespace WinRTWrapper.Test
{
    /// <summary>
    /// Class <see cref="Simple"/> is a simple class.
    /// </summary>
    internal class Simple
    {
        private int _field;

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <param name="index">The index of the value to get or set.</param>
        /// <returns>The value at the specified index.</returns>
        public int this[int index]
        {
            get
            {
                return _field;
            }
            set
            {
                _field = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of <see cref="_field"/>.
        /// </summary>
        public int Property
        {
            get
            {
                return _field;
            }
            set
            {
                _field = value;
            }
        }

        /// <summary>
        /// Gets the value of <see cref="_field"/> without allowing modification.
        /// </summary>
        public int ReadonlyProperty
        {
            get
            {
                return _field;
            }
        }

        /// <summary>
        /// Retrieves the value of the private field <see cref="_field"/>.
        /// </summary>
        /// <returns>The integer value stored in the field <see cref="_field"/>.</returns>
        public int Method()
        {
            return _field;
        }

        /// <summary>
        /// Performs an operation that does not return a value.
        /// </summary>
        public void VoidMethod()
        {
            InternalMethod();
        }

        /// <summary>
        /// Executes an internal operation that does not return a value.
        /// </summary>
        internal void InternalMethod()
        {
            if (Event != null)
            {
                Event(this, _field);
            }
        }

        /// <summary>
        /// Occurs when the associated action is triggered, providing an integer value as event data.
        /// </summary>
        public event EventHandler<int> Event;

        /// <summary>
        /// Other member that does not return a value.
        /// </summary>
        public static void OtherMember() { }

        /// <summary>
        /// Gets a new instance of the <see cref="Simple"/> class with the specified value.
        /// </summary>
        /// <param name="self">The value to initialize the instance with.</param>
        /// <returns>The new instance of <see cref="Simple"/>.</returns>
        [return: WinRTWrapperMarshalUsing(typeof(SimpleWrapper))]
        public static Simple GetSelf([WinRTWrapperMarshalUsing(typeof(SimpleWrapper))] Simple self)
        {
            return self;
        }
    }

    /// <summary>
    /// Class <see cref="GenericSimple{T}"/> is a generic class.
    /// </summary>
    /// <typeparam name="T">The type parameter.</typeparam>
    internal class GenericSimple<T>
    {
        private T _field;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSimple{T}"/> class with the specified value.
        /// </summary>
        /// <param name="field">The value to initialize the instance with. This value is assigned to the internal field.</param>
        public GenericSimple(T field)
        {
            _field = field;
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                return _field;
            }
        }

        /// <summary>
        /// Gets or sets the value of <see cref="_field"/>.
        /// </summary>
        public T Property
        {
            get
            {
                return _field;
            }
            set
            {
                _field = value;
            }
        }

        /// <summary>
        /// Gets the value of <see cref="_field"/> without allowing modification.
        /// </summary>
        public T ReadonlyProperty
        {
            get
            {
                return _field;
            }
        }

        /// <summary>
        /// Retrieves the value of the private field <see cref="_field"/>.
        /// </summary>
        /// <returns>The integer value stored in the field <see cref="_field"/>.</returns>
        public T Method()
        {
            return _field;
        }

        /// <summary>
        /// Performs an operation that does not return a value.
        /// </summary>
        public void VoidMethod()
        {
            InternalMethod();
        }

        /// <summary>
        /// Executes an internal operation that does not return a value.
        /// </summary>
        internal void InternalMethod()
        {
            if (Event != null)
            {
                Event(this, _field);
            }
        }

        /// <summary>
        /// Occurs when the associated action is triggered, providing an integer value as event data.
        /// </summary>
        public event EventHandler<T> Event;

        /// <summary>
        /// Other member that does not return a value.
        /// </summary>
        public static void OtherMember() { }

        /// <summary>
        /// Gets a new instance of the <see cref="GenericSimple{T}"/> class with the specified value.
        /// </summary>
        /// <param name="self">The value to initialize the instance with.</param>
        /// <returns>The new instance of <see cref="GenericSimple{T}"/>.</returns>
        [return: WinRTWrapperMarshalUsing(typeof(GenericSimpleWrapper))]
        public static GenericSimple<T> GetSelf([WinRTWrapperMarshalUsing(typeof(GenericSimpleWrapper))] GenericSimple<T> self)
        {
            return self;
        }
    }

    /// <summary>
    /// Class <see cref="StaticSimple"/> is a static class with properties and methods.
    /// </summary>
    internal static class StaticSimple
    {
        private static int _field;

        /// <summary>
        /// Gets or sets the value of <see cref="_field"/>.
        /// </summary>
        public static int Property
        {
            get
            {
                return _field;
            }
            set
            {
                _field = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of <see cref="_field"/> with a setter that allows modification.
        /// </summary>
        public static int ReadonlyProperty
        {
            get
            {
                return _field;
            }
        }

        /// <summary>
        /// Gets or sets a static property of type <see cref="Simple"/>.
        /// </summary>
        [WinRTWrapperMarshalUsing(typeof(SimpleWrapper))]
        public static Simple SimpleProperty
        {
            get
            {
                return new Simple();
            }
            set
            {
                return;
            }
        }

        /// <summary>
        /// Retrieves the value of the private field <see cref="_field"/>.
        /// </summary>
        /// <returns>The integer value stored in the field <see cref="_field"/>.</returns>
        public static int Method()
        {
            return _field;
        }

        /// <summary>
        /// Performs an operation that does not return a value.
        /// </summary>
        public static void VoidMethod()
        {
            InternalMethod();
        }

        /// <summary>
        /// Executes an internal operation that does not return a value.
        /// </summary>
        internal static void InternalMethod()
        {
            if (Event != null)
            {
                Event(null, _field);
            }

            if (DelegateEvent != null)
            {
                DelegateEvent(new GenericSimple<int>(_field));
            }
        }

        /// <summary>
        /// Occurs when the associated operation triggers an event with an integer value.
        /// </summary>
        public static event EventHandler<int> Event;

        /// <summary>
        /// Occurs when the associated operation triggers an event with a <see cref="GenericSimple{T}"/> argument.
        /// </summary>
        [WinRTWrapperMarshalUsing(typeof(DelegateSimpleMarshaller))]
        public static event Func<GenericSimple<int>, Simple> DelegateEvent;

        /// <summary>
        /// Other member that does not return a value.
        /// </summary>
        public static void OtherMember() { }

        /// <summary>
        /// Gets a new instance of the <see cref="Simple"/> class.
        /// </summary>
        /// <returns>The new instance of <see cref="Simple"/>.</returns>
        [return: WinRTWrapperMarshalUsing(typeof(SimpleWrapper))]
        public static Simple GetSimple()
        {
            return new Simple();
        }
    }

    public delegate SimpleWrapper DelegateSimple(GenericSimpleWrapper arg);

    public interface I
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        int Property {  get; set; }

        /// <summary>
        /// Gets or sets the value with a setter that allows modification.
        /// </summary>
        int ReadonlyProperty { get; }

        /// <summary>
        /// Retrieves the value of the private field.
        /// </summary>
        /// <returns>The integer value stored in the field.</returns>
        int Method();

        /// <summary>
        /// Performs an operation that does not return a value.
        /// </summary>
        void VoidMethod();

        /// <summary>
        /// Occurs when the associated operation triggers an event with an integer value.
        /// </summary>
        event EventHandler<int> Event;
    }

#if NET
    [WinRTWrapperMarshaller(typeof(Simple), typeof(SimpleWrapper))]
#else
    [WinRTWrapperMarshaller(typeof(Simple), typeof(I))]
#endif
    [GenerateWinRTWrapper(typeof(Simple), GenerateMember.Interface)]
    public sealed partial class SimpleWrapper
#if NET
        { }
#else
        : I { }
#endif

    [WinRTWrapperMarshaller(typeof(GenericSimple<int>), typeof(GenericSimpleWrapper))]
    [GenerateWinRTWrapper(typeof(GenericSimple<int>))]
    public sealed partial class GenericSimpleWrapper { }

    [WinRTWrapperMarshaller(typeof(StaticSimple), typeof(StaticSimpleWrapper))]
    [GenerateWinRTWrapper(typeof(StaticSimple))]
    public static partial class StaticSimpleWrapper { }

    [WinRTWrapperMarshaller(typeof(Func<GenericSimple<int>, Simple>), typeof(DelegateSimple))]
    internal class DelegateSimpleMarshaller
    {
        public static DelegateSimple ConvertToWrapper(Func<GenericSimple<int>, Simple> managed)
        {
            return delegate (GenericSimpleWrapper x) { return new SimpleWrapper(managed(GenericSimpleWrapper.ConvertToManaged(x))); };
        }

        public static Func<GenericSimple<int>, Simple> ConvertToManaged(DelegateSimple wrapper)
        {
            return delegate (GenericSimple<int> x) { return SimpleWrapper.ConvertToManaged(wrapper(GenericSimpleWrapper.ConvertToWrapper(x))); };
        }
    }
}
