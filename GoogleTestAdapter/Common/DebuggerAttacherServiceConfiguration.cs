// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.ServiceModel;

namespace GoogleTestAdapter.Common
{
    /// <summary>
    /// Helper class for DebuggerAttacherService.
    /// </summary>
    public class DebuggerAttacherServiceConfiguration
    {
        /// <summary>
        /// Relative address of the endpoint interface.
        /// </summary>
        public static readonly Uri InterfaceAddress = new Uri(nameof(IDebuggerAttacherService), UriKind.Relative);

        /// <summary>
        /// Constructs the Uri at which the service is made available.
        /// </summary>
        /// <param name="id">Identifier of the service end-point</param>
        /// <returns></returns>
        public static Uri ConstructPipeUri(string id)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, "net.pipe://localhost/GTA_{0}/", id));
        }

        /// <summary>
        /// Creates proxy object for the service.
        /// </summary>
        /// <remarks>
        /// The object must be handled as a <ref>System.ServiceModel.IClientChannel</ref>.
        /// </remarks>
        /// <param name="id">Identifier of the service end-point</param>
        /// <param name="timeout">Timeout to use on channel operations</param>
        /// <returns></returns>
        public static IDebuggerAttacherService CreateProxy(string id, TimeSpan timeout)
        {
            var binding = new NetNamedPipeBinding()
            {
                OpenTimeout = timeout,
                CloseTimeout = timeout,
                SendTimeout = timeout,
                ReceiveTimeout = timeout
            };
            var endpointUri = new Uri(ConstructPipeUri(id), InterfaceAddress);
            var endpointAddress = new EndpointAddress(endpointUri);
            return ChannelFactory<IDebuggerAttacherService>.CreateChannel(binding, endpointAddress);
        }

        /// <summary>
        /// Private default constructor to prevent from creating instances of this class.
        /// </summary>
        private DebuggerAttacherServiceConfiguration()
        {
        }
    }
}
