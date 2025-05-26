using WinRTWrapper.CodeAnalysis;

namespace WinRTWrapper.Test
{
    /// <summary>
    /// Class A is a simple class.
    /// </summary>
    internal class A
    {
        private int b;

        /// <summary>
        /// Gets or sets the value of b.
        /// </summary>
        public int B
        {
            get
            {
                return b;
            }
            set
            {
                b = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of b.
        /// </summary>
        public int C
        {
            get
            {
                return b;
            }
        }

        public int D()
        {
            return b;
        }

        public void E() { }

        internal void F() { }
    }

    /// <summary>
    /// Class B is a generic class.
    /// </summary>
    /// <typeparam name="T">The type parameter.</typeparam>
    internal class B<T>
    {
        public B(T a)
        {
            this.a = a;
        }

        private T a;

        /// <summary>
        /// Gets or sets the value of a.
        /// </summary>
        public T A
        {
            get
            {
                return a;
            }
            set
            {
                a = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of b.
        /// </summary>
        public T C
        {
            get
            {
                return a;
            }
        }
    }

    internal static class C
    {
        private static int b;

        /// <summary>
        /// Gets or sets the value of b.
        /// </summary>
        public static int A
        {
            get
            {
                return b;
            }
        }

        /// <summary>
        /// Gets or sets the value of b.
        /// </summary>
        public static int B
        {
            get
            {
                return b;
            }
            set
            {
                b = value;
            }
        }

        public static int D()
        {
            return b;
        }

        public static void E() { }
    }

    [GenerateWinRTWrapper(typeof(A))]
    public partial class Class1 { }

    [GenerateWinRTWrapper(typeof(B<int>))]
    public partial class Class2 { }

    [GenerateWinRTWrapper(typeof(C))]
    public static partial class Class3 { }
}
