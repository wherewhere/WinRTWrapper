using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRTWrapper.CodeAnalysis;

namespace WinRTWrapper.Test
{
    /// <summary>
    /// Class <see cref="Simple"/> is a simple class.
    /// </summary>
    internal class Simple
    {
        /// <summary>
        /// Field that holds an integer value.
        /// </summary>
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
        /// Asynchronously retrieves a <see cref="Task"/> instance.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> instance to retrieve.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>The <see cref="Task"/> instance representing the asynchronous operation.</returns>
        public static Task GetTaskWithTokenAsync(Task task, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return task;
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
        /// <summary>
        /// Field that holds a value of type <typeparamref name="T"/>.
        /// </summary>
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
        /// <summary>
        /// Field that holds an integer value.
        /// </summary>
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

#if COMP_NETSTANDARD2_0
        /// <summary>
        /// Gets or sets a <see cref="System.Drawing.PointF"/> value.
        /// </summary>
        public static System.Drawing.PointF PointProperty
        {
            get
            {
                return new System.Drawing.PointF(1, 2);
            }
            set
            {
                return;
            }
        }

        /// <summary>
        /// Gets or sets the size value represented as a <see cref="System.Drawing.SizeF"/> structure.
        /// </summary>
        public static System.Drawing.SizeF SizeProperty
        {
            get
            {
                return new System.Drawing.SizeF(1, 2);
            }
            set
            {
                return;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="System.Drawing.RectangleF"/> defined by its position and size.
        /// </summary>
        public static System.Drawing.RectangleF RectangleProperty
        {
            get
            {
                return new System.Drawing.RectangleF(1, 2, 3, 4);
            }
            set
            {
                return;
            }
        }
#endif

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

        /// <summary>
        /// Asynchronously retrieves a <see cref="Task"/> instance.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> instance to retrieve.</param>
        /// <returns>The <see cref="Task"/> instance representing the asynchronous operation.</returns>
        public static Task GetTaskAsync(Task task)
        {
            return task;
        }

        /// <summary>
        /// Asynchronously retrieves a <see cref="Task"/> instance.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> instance to retrieve.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>The <see cref="Task"/> instance representing the asynchronous operation.</returns>
        public static Task GetTaskWithTokenAsync(Task task, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return task;
        }

        /// <summary>
        /// Asynchronously retrieves an integer value.
        /// </summary>
        /// <param name="task">The <see cref="Task{TResult}"/> instance to retrieve.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the integer
        /// value retrieved.</returns>
        public static Task<int> GetGenericTaskAsync(Task<int> task)
        {
            return task;
        }

        /// <summary>
        /// Asynchronously retrieves an integer value.
        /// </summary>
        /// <param name="task">The <see cref="Task{TResult}"/> instance to retrieve.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the integer
        /// value retrieved.</returns>
        public static Task<int> GetGenericTaskWithTokenAsync(Task<int> task, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return task;
        }

#if NET
        /// <summary>
        /// Asynchronously retrieves a <see cref="ValueTask"/> instance.
        /// </summary>
        /// <param name="task">The <see cref="ValueTask"/> instance to retrieve.</param>
        /// <returns>The <see cref="ValueTask"/> instance representing the asynchronous operation.</returns>
        public static ValueTask GetValueTaskAsync(ValueTask task)
        {
            return task;
        }

        /// <summary>
        /// Asynchronously retrieves a <see cref="ValueTask"/> instance.
        /// </summary>
        /// <param name="task">The <see cref="ValueTask"/> instance to retrieve.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>The <see cref="ValueTask"/> instance representing the asynchronous operation.</returns>
        public static ValueTask GetValueTaskWithTokenAsync(ValueTask task, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return task;
        }

        /// <summary>
        /// Asynchronously retrieves an integer value.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation. The result contains the integer
        /// value retrieved.</returns>
        public static ValueTask<int> GetGenericValueTaskAsync()
        {
            return ValueTask.FromResult(_field);
        }

        /// <summary>
        /// Asynchronously retrieves an integer value.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation. The result contains the integer
        /// value retrieved.</returns>
        public static ValueTask<int> GetGenericValueTaskWithTokenAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_field);
        }
#endif
    }

    /// <summary>
    /// Struct <see cref="StructSimple"/> is a simple structure that contains properties and methods.
    /// </summary>
    internal struct StructSimple
    {
        /// <summary>
        /// Field that holds an integer value.
        /// </summary>
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
    }

    /// <summary>
    /// Class <see cref="SimpleBase"/> serves as a base class with properties and methods.
    /// </summary>
    internal partial class SimpleBase : IDisposable
    {
        /// <summary>
        /// Gets or sets the value of a property.
        /// </summary>
        public int Property { get; set; }

        /// <inheritdoc/>
        public void Dispose() => GC.SuppressFinalize(this);

        /// <summary>
        /// Retrieves the value of a private field.
        /// </summary>
        /// <returns>The integer value stored in the field.</returns>
        public int Method() => Property;

        /// <summary>
        /// Performs an operation that does not return a value.
        /// </summary>
        public virtual void VirtualMethod() { }
    }

    /// <summary>
    /// Class <see cref="SimpleSub"/> inherits from <see cref="SimpleBase"/> and overrides the virtual method.
    /// </summary>
    internal class SimpleSub : SimpleBase
    {
        /// <inheritdoc/>
        public override void VirtualMethod() { }

        /// <summary>
        /// Gets a new instance of the <see cref="SimpleSub"/> class.
        /// </summary>
        /// <returns>The new instance of <see cref="SimpleSub"/>.</returns>
        [return: WinRTWrapperMarshalUsing(typeof(SimpleBaseWrapper))]
        public SimpleSub GetSelf() => this;
    }

    /// <summary>
    /// Delegate <see cref="DelegateSimple"/> is a delegate that takes a <see cref="GenericSimpleWrapper"/> argument and returns a <see cref="SimpleWrapper"/>.
    /// </summary>
    /// <param name="arg">The argument of type <see cref="GenericSimpleWrapper"/>.</param>
    /// <returns>The result of type <see cref="SimpleWrapper"/>.</returns>
    [WinRTWrapperMarshalling(typeof(DelegateSimpleMarshaller))]
    public delegate SimpleWrapper DelegateSimple(GenericSimpleWrapper arg);

    /// <summary>
    /// Interface <see cref="I"/> defines a contract for a simple interface with properties, methods, and events.
    /// </summary>
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
#if NET
    [GenerateWinRTWrapper(typeof(Simple), typeof(I))]
#else
    [GenerateWinRTWrapper(typeof(Simple), GenerateMember.Interface)]
#endif
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

    [WinRTWrapperMarshaller(typeof(StructSimple), typeof(StructSimpleWrapper))]
    [GenerateWinRTWrapper(typeof(StructSimple))]
    public sealed partial class StructSimpleWrapper { }

    [WinRTWrapperMarshaller(typeof(SimpleBase), typeof(SimpleBaseWrapper))]
    [GenerateWinRTWrapper(typeof(SimpleBase))]
    public sealed partial class SimpleBaseWrapper : IDisposable
    {
        public partial void Dispose();
    }

    [WinRTWrapperMarshaller(typeof(SimpleSub), typeof(SimpleSubWrapper))]
    [GenerateWinRTWrapper(typeof(SimpleSub))]
    public sealed partial class SimpleSubWrapper : IDisposable
    {
        public partial void Dispose();
    }

    [WinRTWrapperMarshaller(typeof(Simple), typeof(DefinedSimpleWrapper))]
    [GenerateWinRTWrapper(typeof(Simple), GenerateMember.Defined)]
    public sealed partial class DefinedSimpleWrapper
    {
        public partial int Method();

        public static partial IAsyncAction GetTaskWithTokenAsync(IAsyncAction task);

#if !NET
        public static partial DefinedSimpleWrapper GetSelf(DefinedSimpleWrapper self);
#endif
    }

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
