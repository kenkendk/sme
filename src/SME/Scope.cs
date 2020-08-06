using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SME
{
    /// <summary>
    /// Scopes for handling nesting of similar typed processes and buses.
    /// </summary>
    public class Scope : IDisposable
    {
        /// <summary>
        /// A flag keeping the disposed state for the scope.
        /// </summary>
        private bool m_disposed = false;

        /// <summary>
        /// The parent scope.
        /// </summary>
        private readonly Scope m_parent;
        /// <summary>
        /// The call context key for this scope.
        /// </summary>
        private readonly string m_scopeKey;
        /// <summary>
        /// A flag indicating if this scope is isolated.
        /// </summary>
        private readonly bool m_isolated;

        /// <summary>
        /// The clock used in this instance.
        /// </summary>
        private readonly Clock m_clock;

        /// <summary>
        /// The lookup table for busses.
        /// </summary>
        private readonly Dictionary<string, IRuntimeBus> m_busses = new Dictionary<string, IRuntimeBus>();

        /// <summary>
        /// The lookup table for processes.
        /// </summary>
        private readonly Dictionary<string, IProcess> m_processes = new Dictionary<string, IProcess>();

        /// <summary>
        /// Gets or sets the clock used in this scope.
        /// </summary>
        /// <value>The clock.</value>
        public Clock Clock => m_clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.NamingScope"/> class.
        /// </summary>
        /// <param name="isolated">If set to <c>true</c> this scope is isolated.</param>
        /// <param name="clock">The clock to use, if not using the inherited clock.</param>
        public Scope(bool isolated = true, Clock clock = null)
            : this(Current, isolated, clock, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.NamingScope"/> class.
        /// </summary>
        /// <param name="parent">The scope parent.</param>
        /// <param name="isolated">If set to <c>true</c> this scope is isolated.</param>
        /// <param name="isRoot">If set to <c>true</c> this scope is the root scope.</param>
        /// <param name="clock">The clock to use if not using the inherited clock.</param>
        private Scope(Scope parent, bool isolated, Clock clock, bool isRoot)
        {
            if (!isRoot && parent == null)
                throw new ArgumentNullException(nameof(parent));

            m_parent = parent;
            m_isolated = isolated;
            ScopeKey = m_scopeKey = (isRoot ? "ROOT" : Guid.NewGuid().ToString());
            m_clock = isRoot ? new Clock() : (clock ?? m_parent.Clock);
            Current = this;
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:SME.NamingScope"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:SME.NamingScope"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:SME.NamingScope"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="T:SME.NamingScope"/> so the garbage
        /// collector can reclaim the memory that the <see cref="T:SME.NamingScope"/> was occupying.</remarks>
        public void Dispose()
        {
            if (!m_disposed)
            {
                if (this == ROOT_SCOPE)
                    throw new InvalidOperationException("Cannot dispose the root scope");

                m_disposed = true;

                if (Current == this)
                {
                    Current = null;

                    var parent = m_parent;
                    ScopeKey = parent.m_scopeKey;
                    while (parent != null && parent.m_disposed)
                    {
                        parent = parent.m_parent;

                        Current = null;
                        ScopeKey = parent.m_scopeKey;
                    }
                }
            }
        }

        /// <summary>
        /// Registers an existing bus with the given name in this scope.
        /// </summary>
        /// <returns>The registered bus.</returns>
        /// <param name="name">The name of the bus to register.</param>
        /// <param name="bus">The bus to register.</param>
        /// <typeparam name="T">The bus type.</typeparam>
        public static T RegisterBus<T>(string name, T bus) where T : class, IBus
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            if (typeof(T) == typeof(IBus))
                throw new ArgumentException($"Cannot create a bus of type {typeof(T).FullName}");
            if (!typeof(IRuntimeBus).IsAssignableFrom(typeof(T)))
                throw new ArgumentException($"Cannot register a non-runtime bus");

            Current.m_busses[name] = (IRuntimeBus)bus;

            return bus;
        }

        /// <summary>
        /// Creates a new bus with the given name.
        /// </summary>
        /// <returns>The created bus.</returns>
        /// <param name="name">The name of the bus to create.</param>
        /// <typeparam name="T">The bus type.</typeparam>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        public static T CreateBus<T>(string name = null, bool internalBus = false) where T : class, IBus
        {
            return (T)CreateBus(typeof(T), name, internalBus);
        }

        /// <summary>
        /// Creates a new bus with the given name.
        /// </summary>
        /// <returns>The created bus.</returns>
        /// <param name="name">The name of the bus to create.</param>
        /// <param name="bustype">The bus type.</param>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        public static IBus CreateBus(Type bustype, string name = null, bool internalBus = false)
        {
            return CreateOrLoadBus(bustype, name, internalBus, true);
        }

        /// <summary>
        /// Finds the bus with the given name and type, or creates a new if none is found.
        /// </summary>
        /// <returns>The loaded or created bus.</returns>
        /// <param name="name">The name of the bus to find.</param>
        /// <typeparam name="T">The bus type.</typeparam>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        public static T LoadBus<T>(string name = null, bool internalBus = false) where T : class, IBus
        {
            return (T)LoadBus(typeof(T), name, internalBus);
        }

        /// <summary>
        /// Finds the bus with the given name and type, or creates a new if none is found.
        /// </summary>
        /// <returns>The loaded or created bus.</returns>
        /// <param name="name">The name of the bus to find.</param>
        /// <param name="bustype">The bus type.</param>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        public static IBus LoadBus(Type bustype, string name = null, bool internalBus = false)
        {
            var scope = GetTargetScope(bustype, ref name, internalBus);
            if (scope.FindBus(name) == null)
                throw new Exception($"Unable to find the bus, make sure it is created before calling {nameof(LoadBus)}");

            return CreateOrLoadBus(bustype, name, internalBus, false);
        }

        /// <summary>
        /// Finds the bus with the given name and type, or creates a new if none is found.
        /// </summary>
        /// <returns>The loaded or created bus.</returns>
        /// <param name="name">The name of the bus to find.</param>
        /// <typeparam name="T">The bus type.</typeparam>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        /// <param name="forceCreate">A flag indicating if the bus should be created even if it exists.</param>
        public static T CreateOrLoadBus<T>(string name = null, bool internalBus = false, bool forceCreate = false) where T : class, IBus
        {
            return (T)CreateOrLoadBus(typeof(T), name, internalBus, forceCreate);
        }

        /// <summary>
        /// Gets the target scope for a bus.
        /// </summary>
        /// <returns>The target scope.</returns>
        /// <param name="name">The name of the bus to find.</param>
        /// <param name="bustype">The bus type.</param>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        private static Scope GetTargetScope(Type bustype, ref string name, bool internalBus)
        {
            Scope targetScope;
            var isClocked = (bustype.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() as ClockedBusAttribute) != null;
            if (internalBus)
            {
                if (name != null)
                    throw new ArgumentException($"Cannot set the name of an internal bus");
                return null;
            }
            else if (typeof(ISingletonBus).IsAssignableFrom(bustype))
            {
                if (name != null)
                    throw new ArgumentException($"Cannot set the name of a {nameof(ISingletonBus)}");
                name = bustype.FullName;
                targetScope = ROOT_SCOPE;
            }
            else
            {
                if (name == null)
                    name = bustype.FullName;
                targetScope = Current;
            }

            return targetScope;
        }

        /// <summary>
        /// Finds the bus with the given name and type, or creates a new if none is found.
        /// </summary>
        /// <returns>The loaded or created bus.</returns>
        /// <param name="name">The name of the bus to find.</param>
        /// <param name="bustype">The bus type.</param>
        /// <param name="internalBus">A flag indicating if this is an internal bus.</param>
        /// <param name="forceCreate">A flag indicating if the bus should be created even if it exists.</param>
        public static IBus CreateOrLoadBus(Type bustype, string name = null, bool internalBus = false, bool forceCreate = false)
        {
            var targetScope = GetTargetScope(bustype, ref name, internalBus);
            var isClocked = (bustype.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() as ClockedBusAttribute) != null;

            if (targetScope == null)
                return Bus.CreateFromInterface(bustype, Current.Clock, isClocked, true);

            if (bustype == typeof(IBus) || bustype == typeof(ISingletonBus))
                throw new ArgumentException($"Cannot create a bus of type {bustype.FullName}");

            var res = forceCreate ? null : targetScope.FindBus(name);
            if (res != null && res.BusType != bustype)
                throw new InvalidOperationException($"Attempted to create a bus with the name {name} and the type {bustype.FullName}, but a bus is already registered under that name with the type {res.GetType().FullName}");

            if (res == null)
                targetScope.m_busses[name] = res = Bus.CreateFromInterface(bustype, targetScope.Clock, isClocked, false);

            return res;
        }

        /// <summary>
        /// Creates an internal bus.
        /// </summary>
        /// <returns>The internal bus.</returns>
        /// <typeparam name="T">The bus type.</typeparam>
        public static T CreateInternalBus<T>() where T : class, IBus
        {
            return CreateOrLoadBus<T>(null, internalBus: true);
        }

        /// <summary>
        /// Finds a named bus by searching the scope hierarchy.
        /// </summary>
        /// <returns>The bus or null.</returns>
        /// <param name="name">The name of the bus to find.</param>
        internal IRuntimeBus FindBus(string name)
        {
            return RecursiveLookup(x => x.m_busses, name);
        }
        /// <summary>
        /// Finds a named process by searching the scope hierarchy.
        /// </summary>
        /// <returns>The process or null.</returns>
        /// <param name="name">The name of the process to find.</param>
        internal IProcess FindProcess(string name)
        {
            return RecursiveLookup(x => x.m_processes, name);
        }

        /// <summary>
        /// Finds a named item by searching the scope hierarchy.
        /// </summary>
        /// <returns>The matching item or null.</returns>
        /// <param name="dict_fun">The function used to extract the dictionary.</param>
        /// <param name="name">The name of the item to find.</param>
        /// <typeparam name="T">The type of the element to find.</typeparam>
        internal T RecursiveLookup<T>(Func<Scope, Dictionary<string, T>> dict_fun, string name)
        {
            T res = default(T);
            var cur = this;
            while (cur != null)
            {
                var dict = dict_fun(cur);
                if (dict.TryGetValue(name, out res))
                    break;

                cur = cur.m_isolated ? null : cur.m_parent;
            }

            return res;
        }

        #region "Static members giving access to the call context"
        /// <summary>
        /// The one and only root scope.
        /// </summary>
        private static readonly Scope ROOT_SCOPE;

        /// <summary>
        /// Static initializer for the <see cref="T:SME.Scope"/> class.
        /// </summary>
        static Scope()
        {
            // This is done to enforce the order of initializing the fields,
            // as the ROOT_SCOPE variable depends on the _scope being
            // initialized

            _scopes = new Dictionary<string, Scope>();
            ROOT_SCOPE = new Scope(null, true, null, true);
        }

        /// <summary>
        /// Gets the current scope.
        /// </summary>
        /// <value>The current scope.</value>
        public static Scope Current
        {
            get
            {
                var key = ScopeKey;
                if (key == null)
                    return ROOT_SCOPE;

                Scope res;
                _scopes.TryGetValue(key, out res);
                return res ?? ROOT_SCOPE;
            }

            private set
            {
                var key = ScopeKey;
                if (key == null)
                    throw new InvalidOperationException("Cannot set the scope without a key");
                if (value == null)
                    _scopes.Remove(key);
                else
                    _scopes[key] = value;
            }
        }

        /// <summary>
        /// The scopes matching the keys.
        /// </summary>
        private static readonly Dictionary<string, Scope> _scopes;

        /// <summary>
        /// The shared scope key.
        /// </summary>
        private static AsyncLocal<string> _scopekey = new AsyncLocal<string>();

        /// <summary>
        /// Gets or sets the scope key from the call context.
        /// </summary>
        /// <value>The scope key.</value>
        private static string ScopeKey
        {
            get => _scopekey.Value;
            set => _scopekey.Value = value;
        }
        #endregion
    }
}
