using System;
using ModestTree;

namespace Zenject
{
    public class BindSignalToBinder<TSignal>
    {
        DiContainer _container;
        BindStatement _bindStatement;

        public BindSignalToBinder(DiContainer container)
        {
            _container = container;

            // This will ensure that they finish the binding
            _bindStatement = container.StartBinding();
        }

        public SignalCopyBinder ToMethod(Action<TSignal> callback)
        {
            Assert.That(!_bindStatement.HasFinalizer);
            _bindStatement.SetFinalizer(new NullBindingFinalizer());

            var bindInfo = _container.Bind<IDisposable>()
                .To<SignalCallbackWrapper>()
                .AsCached()
                // Note that there's a reason we don't just make SignalCallbackWrapper have a generic
                // argument for signal type - because when using struct type signals it throws
                // exceptions on AOT platforms
                .WithArguments(typeof(TSignal), (Action<object>)((o) => callback((TSignal)o)))
                .NonLazy().BindInfo;

            return new SignalCopyBinder(bindInfo);
        }

        public SignalCopyBinder ToMethod(Action callback)
        {
            return ToMethod(signal => callback());
        }

        public BindSignalFromBinder<TObject, TSignal> ToMethod<TObject>(Action<TObject, TSignal> handler)
        {
            return ToMethod<TObject>(x => (Action<TSignal>)(s => handler(x, s)));
        }

        public BindSignalFromBinder<TObject, TSignal> ToMethod<TObject>(Func<TObject, Action> handlerGetter)
        {
            return ToMethod<TObject>(x => (Action<TSignal>)(s => handlerGetter(x)()));
        }

        public BindSignalFromBinder<TObject, TSignal> ToMethod<TObject>(Func<TObject, Action<TSignal>> handlerGetter)
        {
            return new BindSignalFromBinder<TObject, TSignal>(
                _bindStatement, handlerGetter, _container);
        }
    }
}
