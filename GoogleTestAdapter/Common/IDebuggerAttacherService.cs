// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace GoogleTestAdapter.Common
{
    /// <summary>
    /// Interface of DebuggerAttacherService.
    /// </summary>
    [ServiceContract]
    public interface IDebuggerAttacherService
    {
        /// <summary>
        /// Attaches the debugger to the specified process.
        /// </summary>
        /// <param name="processId">ID of a process to attach to</param>
        [OperationContract]
        [FaultContract(typeof(DebuggerAttacherServiceFault))]
        void AttachDebugger(int processId);
    }

    /// <summary>
    /// Fault reported from DebuggerAttacherService.
    /// </summary>
    [DataContract]
    public class DebuggerAttacherServiceFault
    {
        public DebuggerAttacherServiceFault(string message)
        {
            Message = message;
        }

        [DataMember]
        public string Message { get; private set; }
    }

    /// <summary>
    /// Abstract wrapper around IDebuggerAttacherService.
    /// </summary>
    public interface IDebuggerAttacherServiceWrapper : IDisposable
    {
        /// <summary>
        /// The wrapped object.
        /// </summary>
        IDebuggerAttacherService Service { get; }
    }

    /// <summary>
    /// Wrapper around IDebuggerAttacherService channel proxy.
    /// </summary>
    public class DebuggerAttacherServiceProxyWrapper : IDebuggerAttacherServiceWrapper
    {
        public DebuggerAttacherServiceProxyWrapper(IDebuggerAttacherService proxy)
        {
            Service = proxy;
        }

        public IDebuggerAttacherService Service { get; private set; }

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        ((IClientChannel)Service).Close();
                    }
                    catch (CommunicationException)
                    {
                        ((IClientChannel)Service).Abort();
                        // Not rethrowing CommunicationException
                    }
                    catch (Exception)
                    {
                        ((IClientChannel)Service).Abort();
                        throw;
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
