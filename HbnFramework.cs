using System;
using System.Collections.Generic;
using UnityEngine;

namespace HbnFramework
{
    #region Architecture
    public interface IArchitecture
    {
        #region System,Model,Utility
        void RegisterModel<TModel>(TModel model) where TModel : IModel;
        void RegisterUtility<TUtility>(TUtility instance) where TUtility : IUtility;
        void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem;
        TModel GetModel<TModel>() where TModel : class, IModel;
        TUtility GetUtility<TUtility>() where TUtility : class, IUtility;
        TSystem GetSystem<TSystem>() where TSystem : class, ISystem;
        #endregion

        #region Command
        void SendCommand<TCommand>() where TCommand : ICommand, new();
        void SendCommand<TCommand>(TCommand command) where TCommand : ICommand;
        #endregion

        #region Event
        void SendEvent<TEvent>() where TEvent : new();
        void SendEvent<TEvent>(TEvent e);
        IUnregister RegisterEvent<TEvent>(Action<TEvent> onEvent);
        void UnRegisterEvent<TEvent>(Action<TEvent> onEvent);
        #endregion

        #region Query
        TResult SendQuery<TResult>(IQuery<TResult> query);
        #endregion
    }

    /// <summary>
    /// �ܹ�
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
    {
        /// <summary>
        /// �Ƿ��ʼ�����
        /// </summary>
        private bool isInit = false;

        public static Action<T> OnRegisterPatch = architecture => { };
        public static IArchitecture Interface
        {
            get
            {
                if (mArchitecture == null)
                {
                    MakeSureArchitecture();
                }
                return mArchitecture;
            }

        }

        private List<IModel> mModels = new List<IModel>();
        private List<ISystem> mSystems = new List<ISystem>();

        public void RegisterSystem<T>(T instance) where T : ISystem
        {
            // ��Ҫ�� Model ��ֵһ��
            instance.SetArchitecture(this); // �޸�

            mContainer.Register<T>(instance);

            // �����ʼ������
            if (isInit)
            {
                instance.Init();
            }
            else
            {
                // ��ӵ� Model �����У����ڳ�ʼ��
                mSystems.Add(instance);
            }
        }

        public void RegisterModel<T>(T instance) where T : IModel
        {
            // ��Ҫ�� Model ��ֵһ��
            instance.SetArchitecture(this); // �޸�
            mContainer.Register<T>(instance);

            // �����ʼ������
            if (isInit)
            {
                instance.Init();
            }
            else
            {
                // ��ӵ� Model �����У����ڳ�ʼ��
                mModels.Add(instance);
            }
        }

        public void RegisterUtility<T>(T instance) where T : IUtility
        {
            mArchitecture.mContainer.Register<T>(instance);
        }

        #region ���Ƶ���ģʽ ���ǽ����ڲ�����
        private static T mArchitecture = null;

        // ȷ�� Container ����ʵ����
        static void MakeSureArchitecture()
        {
            if (mArchitecture == null)
            {
                mArchitecture = new T();
                mArchitecture.Init();

                // ����
                OnRegisterPatch?.Invoke(mArchitecture);

                // ��ʼ�� Model
                foreach (var architectureModel in mArchitecture.mModels)
                {
                    architectureModel.Init();
                }
                // ��� Model
                mArchitecture.mModels.Clear();

                // ��ʼ�� System
                foreach (var architectureSystem in mArchitecture.mSystems)
                {
                    architectureSystem.Init();
                }
                // ��� System
                mArchitecture.mSystems.Clear();

                mArchitecture.isInit = true;
            }
        }
        #endregion

        private IOCContainer mContainer = new IOCContainer();

        // ��������ע��ģ��
        protected abstract void Init();
        // ע��Utility
        public static void Register<T>(T instance)
        {
            MakeSureArchitecture();
            mArchitecture.mContainer.Register<T>(instance);
        }

        // �ṩһ����ȡģ��� API
        public static T Get<T>() where T : class
        {
            MakeSureArchitecture();
            return mArchitecture.mContainer.Get<T>();
        }

        public T GetModel<T>() where T : class, IModel
        {
            return mContainer.Get<T>();
        }
        public T GetUtility<T>() where T : class, IUtility
        {
            return mContainer.Get<T>();
        }

        public T GetSystem<T>() where T : class, ISystem
        {
            return mContainer.Get<T>();
        }

        public void SendCommand<T>() where T : ICommand, new()
        {
            var command = new T();
            command.SetArchitecture(this);
            command.Execute();
            command.SetArchitecture(null);
        }

        public void SendCommand<T>(T command) where T : ICommand
        {
            command.SetArchitecture(this);
            command.Execute();
            command.SetArchitecture(null);
        }

        private ITypeEventSystem mTypeEventSystem = new TypeEventSystem();

        public void SendEvent<T>() where T : new()
        {
            mTypeEventSystem.Send<T>();
        }

        public void SendEvent<T>(T e)
        {
            mTypeEventSystem.Send<T>(e);
        }

        public IUnregister RegisterEvent<T>(Action<T> onEvent)
        {
            return mTypeEventSystem.Register<T>(onEvent);
        }

        public void UnRegisterEvent<T>(Action<T> onEvent)
        {
            mTypeEventSystem.Unregister<T>(onEvent);
        }

        public TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            query.SetArchitecture(this);
            return query.Do();
        }
    }
    #endregion

    #region Controller
    public interface IController : ICanGetArchitecture, ICanSendCommand, ICanGetSystem, ICanGetModel, ICanRegisterEvent
    {

    }
    #endregion

    #region System
    public interface ISystem : ICanGetArchitecture, ICanSetArchitecture,ICanGetSystem, ICanGetModel, ICanGetUtility, ICanSendEvent, ICanRegisterEvent
    {
        void Init();
    }

    public abstract class AbstractSystem : ISystem
    {
        private IArchitecture mArchitecture = null;
        IArchitecture ICanGetArchitecture.GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        void ISystem.Init()
        {
            OnInit();
        }

        public abstract void OnInit();
    }
    #endregion

    #region Model
    public interface IModel : ICanGetArchitecture, ICanSetArchitecture, ICanGetUtility, ICanSendEvent
    {
        void Init();
    }

    public abstract class AbstractModel : IModel
    {
        private IArchitecture mArchitecture = null;

        IArchitecture ICanGetArchitecture.GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        void IModel.Init()
        {
            OnInit();
        }

        public abstract void OnInit();
    }
    #endregion

    #region Utility
    public interface IUtility
    {

    }
    #endregion

    #region Command
    public interface ICommand : ICanGetArchitecture, ICanSetArchitecture, ICanGetSystem, ICanGetModel, ICanGetUtility, ICanRegisterEvent, ICanSendEvent
    {
        void Execute();
    }

    public abstract class AbstractCommand : ICommand
    {
        private IArchitecture mArchitecture;

        void ICommand.Execute()
        {
            onExecute();
        }

        IArchitecture ICanGetArchitecture.GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        public abstract void onExecute();
    }
    #endregion

    #region Query
public interface IQuery<T> : ICanGetArchitecture, ICanSetArchitecture, ICanGetModel, ICanGetSystem, ICanGetUtility
    {
        T Do();
    }

    public abstract class AbstractQuery<T> : IQuery<T>
    {
        public T Do()
        {
            return OnDo();
        }

        protected abstract T OnDo();

        private IArchitecture mArchitecture;
        public IArchitecture GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }
    }
    #endregion

    #region Rules

    #region Architecture
    public interface ICanGetArchitecture
    {
        IArchitecture GetArchitecture();
    }

    public interface ICanSetArchitecture
    {
        void SetArchitecture(IArchitecture architecture);
    }
    #endregion
    #region System
    public interface ICanGetSystem : ICanGetArchitecture
    {

    }

    public static class GetSystemExtensions
    {
        public static T GetSystem<T>(this ICanGetSystem self) where T : ISystem
        {
            return self.GetSystem<T>();
        }
    }
    #endregion
    #region Model
    public interface ICanGetModel : ICanGetArchitecture
    {

    }

    public static class CanGetModelExtensions
    {
        public static T GetModel<T>(this ICanGetModel self) where T : class, IModel
        {
            return self.GetArchitecture().GetModel<T>();
        }
    }
    #endregion
    #region Utility
    public interface ICanGetUtility : ICanGetArchitecture
    {

    }

    public static class CanGetUtilityExtensions
    {
        public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility
        {
            return self.GetArchitecture().GetUtility<T>();
        }
    }
    #endregion

    #region Command
    public interface ICanSendCommand : ICanGetArchitecture
    {

    }

    public static class SendCommandExtensions
    {
        public static void SendCommand<T>(this ICanSendCommand self) where T : ICommand, new()
        {
            self.GetArchitecture().SendCommand<T>();
        }

        public static void SendCommand<T>(this ICanSendCommand self, T command) where T : ICommand
        {
            self.GetArchitecture().SendCommand<T>(command);
        }
    }
    #endregion
    #region Event
    public interface ICanRegisterEvent : ICanGetArchitecture
    {

    }

    public static class CanRegisterEventExtension
    {
        public static IUnregister RegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            return self.GetArchitecture().RegisterEvent<T>(onEvent);
        }

        public static void UnregisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            self.GetArchitecture().UnRegisterEvent<T>(onEvent);
        }
    }

    public interface ICanSendEvent : ICanGetArchitecture
    {

    }

    public static class CanSendEventExtension
    {
        public static void SendEvent<T>(this ICanSendEvent self) where T : new()
        {
            self.GetArchitecture().SendEvent<T>();
        }

        public static void SendEvent<T>(this ICanSendEvent self, T e)
        {
            self.GetArchitecture().SendEvent<T>(e);
        }
    }
    #endregion
    #region Query
    public interface ICanSendQuery : ICanGetArchitecture { }

    public static class CanSendQueryExtension
    {
        public static T SendQuery<T>(this ICanSendQuery self, IQuery<T> query)
        {
            return self.GetArchitecture().SendQuery(query);
        }
    }
    #endregion

    #endregion

    #region TypeEventSystem
    public interface ITypeEventSystem
    {
        void Send<T>() where T : new();
        void Send<T>(T e);

        IUnregister Register<T>(Action<T> onEvent);
        void Unregister<T>(Action<T> onEvent);
    }

    public interface IUnregister
    {
        void Unregister();
    }

    /// <summary>
    /// ע����ʵ��
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypeEventSystemUnregister<T> : IUnregister
    {
        public ITypeEventSystem System { get; set; }
        public Action<T> onEvent { get; set; }
        public void Unregister()
        {
            System.Unregister(onEvent);

            System = null;
            onEvent = null;
        }
    }


    /// <summary>
    /// �ⲿ��ֱ�ӹ���GameObject��
    /// һ��GameObject�����٣��Զ�ע����Ӧ�¼�
    /// </summary>
    public class UnRegisterOnDestroyTrigger : MonoBehaviour
    {
        private HashSet<IUnregister> mUnRegisters = new HashSet<IUnregister>();

        public void AddUnRegister(IUnregister unRegister)
        {
            mUnRegisters.Add(unRegister);
        }

        private void OnDestroy()
        {
            foreach (var unRegister in mUnRegisters)
            {
                unRegister.Unregister();
            }

            mUnRegisters.Clear();
        }
    }

    /// <summary>
    /// ע����������ʹ�ü�
    /// </summary>
    public static class UnRegisterExtension
    {
        public static void UnRegisterWhenGameObjectDestroyed(this IUnregister unRegister, GameObject gameObject)
        {
            var trigger = gameObject.GetComponent<UnRegisterOnDestroyTrigger>();

            if (!trigger)
            {
                trigger = gameObject.AddComponent<UnRegisterOnDestroyTrigger>();
            }

            trigger.AddUnRegister(unRegister);
        }
    }

    public class TypeEventSystem : ITypeEventSystem
    {
        interface IRegistrations
        {

        }

        class Registrations<T> : IRegistrations
        {
            /// <summary>
            /// ��Ϊί�б���Ϳ���һ�Զ�ע��
            /// </summary>
            public Action<T> OnEvent = obj => { };
        }

        private Dictionary<Type, IRegistrations> mEventRegistrations = new Dictionary<Type, IRegistrations>();

        public static readonly TypeEventSystem Global = new TypeEventSystem();


        public void Send<T>() where T : new()
        {
            var e = new T();
            Send<T>(e);
        }

        public void Send<T>(T e)
        {
            var type = typeof(T);
            IRegistrations eventRegistrations;

            if (mEventRegistrations.TryGetValue(type, out eventRegistrations))
            {
                (eventRegistrations as Registrations<T>)?.OnEvent.Invoke(e);
            }

        }

        public IUnregister Register<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            IRegistrations eventRegistrations;

            if (mEventRegistrations.TryGetValue(type, out eventRegistrations))
            {

            }
            else
            {
                eventRegistrations = new Registrations<T>();
                mEventRegistrations.Add(type, eventRegistrations);
            }

            (eventRegistrations as Registrations<T>).OnEvent += onEvent;

            return new TypeEventSystemUnregister<T>()
            {
                onEvent = onEvent,
                System = this
            };
        }

        public void Unregister<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            IRegistrations eventRegistrations;

            if (mEventRegistrations.TryGetValue(type, out eventRegistrations))
            {
                (eventRegistrations as Registrations<T>).OnEvent -= onEvent;
            }
        }
    }

    public interface IOnEvent<T>
    {
        void OnEvent(T e);
    }

    public static class OnGlobalEventExtension
    {
        public static IUnregister RegisterEvent<T>(this IOnEvent<T> self) where T : struct
        {
            return TypeEventSystem.Global.Register<T>(self.OnEvent);
        }

        public static void UnregisterEvent<T>(this IOnEvent<T> self) where T : struct
        {
            TypeEventSystem.Global.Unregister<T>(self.OnEvent);
        }
    }
    #endregion

    #region IOC
    public class IOCContainer
    {
        public Dictionary<Type, object> mInstances = new Dictionary<Type, object>();

        /// <summary>
        /// ע��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public void Register<T>(T instance)
        {
            var key = typeof(T);

            // �����ڣ����;��������
            if (mInstances.ContainsKey(key))
            {
                mInstances[key] = instance;
            }
            else
            {
                mInstances.Add(key, instance);
            }
        }

        /// <summary>
        /// ��ȡ
        /// </summary>
        public T Get<T>() where T : class
        {
            var key = typeof(T);

            object retObj;

            if (mInstances.TryGetValue(key, out retObj))
            {
                return retObj as T;
            }

            return null;
        }
    }
    #endregion

    #region BindableProperty
    /// <summary>
    /// �ɰ󶨵�����
    /// Ŀǰֻ�ǽ����ݺ����ݱ���¼�����һ��
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BindableProperty<T> where T : IEquatable<T>
    {
        public BindableProperty(T defaultValue = default)
        {
            mValue = defaultValue;
        }

        private T mValue;
        public T Value
        {
            get => mValue;
            set
            {
                if (Value == null && mValue == null) return;


                if (Value != null && mValue.Equals(value)) return;
               
                mValue = value;
                mOnValueChanged?.Invoke(value);
                
            }
        }

        private Action<T> mOnValueChanged = (v) => { }; 

        public IUnregister Register(Action<T> onValueChanged) 
        {
            mOnValueChanged += onValueChanged;
            return new BindablePropertyUnregister<T>()
            {
                BindableProperty = this,
                OnValueChanged = onValueChanged
            };
        }

        public IUnregister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(mValue);
            return Register(onValueChanged);
        }

        public void Unregister(Action<T> onValueChanged) 
        {
            mOnValueChanged -= onValueChanged;
        }

        /// <summary>
        /// Ĭ�Ϸ���Valueֵ
        /// </summary>
        /// <param name="property"></param>
        public static implicit operator T(BindableProperty<T> property)
        {
            return property.Value;
        }
    }

    public class BindablePropertyUnregister<T> : IUnregister where T : IEquatable<T> 
    {
        public BindableProperty<T> BindableProperty { get; set; }

        public Action<T> OnValueChanged { get; set; }

        public void Unregister()
        {
            BindableProperty.Unregister(OnValueChanged);
        }
    }
    #endregion
}
