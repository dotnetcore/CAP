using System;
using System.Reflection;

namespace Cap.Consistency.EventBus
{
    public class EventHandlerHolder
        : IDisposable
    {
        public object Handler { get; }

        public MethodInfo MethodInfo { get; }

        public Type ParameterType { get; }

        public EventHandlerHolder(object handler, MethodInfo methodInfo, Type parameterType) {
            Handler = handler;
            MethodInfo = methodInfo;
            ParameterType = parameterType;
        }

        #region IDisposable

        // Flag: Has Dispose already been called?
        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        #endregion IDisposable
    }
}