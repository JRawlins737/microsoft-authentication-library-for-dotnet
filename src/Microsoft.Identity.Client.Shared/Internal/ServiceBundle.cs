﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Http;

namespace Microsoft.Identity.Client.Internal
{
    internal class ServiceBundle : IServiceBundle
    {
        internal ServiceBundle(
            ApplicationConfiguration config,
            IPlatformProxy platformProxy,
            bool shouldClearCaches = false)
        {
            Config = config;
            PlatformProxy = config.PlatformProxy ?? platformProxy;

            DefaultLogger = new MsalLogger(
                PlatformProxy,
                Guid.Empty,
                config.ClientName,
                config.ClientVersion,
                config.LogLevel,
                config.EnablePiiLogging,
                config.IsDefaultPlatformLoggingEnabled,
                config.LoggingCallback);

            HttpManager = config.HttpManager ?? new HttpManager(
                config.HttpClientFactory ?? 
                PlatformProxy.CreateDefaultHttpClientFactory());

            HttpTelemetryManager = new HttpTelemetryManager();
            if (config.TelemetryConfig != null)
            {
                // This can return null if the device isn't sampled in.  There's no need for processing MATS events if we're not going to send them.
                Mats = TelemetryClient.CreateMats(config, PlatformProxy, config.TelemetryConfig);
                MatsTelemetryManager = Mats?.TelemetryManager ??
                    new TelemetryManager(config, PlatformProxy, config.TelemetryCallback);
            }
            else
            {
                MatsTelemetryManager = new TelemetryManager(config, PlatformProxy, config.TelemetryCallback);
            }

            InstanceDiscoveryManager = new InstanceDiscoveryManager(
                HttpManager,
                MatsTelemetryManager,
                shouldClearCaches,
                config.CustomInstanceDiscoveryMetadata,
                config.CustomInstanceDiscoveryMetadataUri);

            ThrottlingManager = SingletonThrottlingManager.GetInstance();
            AuthorityEndpointResolutionManager = new AuthorityEndpointResolutionManager(this, shouldClearCaches);
            DeviceAuthManager = PlatformProxy.CreateDeviceAuthManager();
        }

        /// <summary>
        /// This logger does not contain a correlation ID and should be used only when the correlation ID is not available
        /// i.e. before a request exists
        /// </summary>
        public ICoreLogger DefaultLogger { get; }

        /// <inheritdoc />
        public IHttpManager HttpManager { get; }

        /// <inheritdoc />
        public IMatsTelemetryManager MatsTelemetryManager { get; }

        public IInstanceDiscoveryManager InstanceDiscoveryManager { get; }

        /// <inheritdoc />
        public IAuthorityEndpointResolutionManager AuthorityEndpointResolutionManager { get; }

        /// <inheritdoc />
        public IPlatformProxy PlatformProxy { get; private set; }

        /// <inheritdoc />
        public IApplicationConfiguration Config { get; }

        /// <inheritdoc />
        public ITelemetryClient Mats { get; }

        public IDeviceAuthManager DeviceAuthManager { get; }

        public IHttpTelemetryManager HttpTelemetryManager { get; }

        public IThrottlingProvider ThrottlingManager { get; }

        public static ServiceBundle Create(ApplicationConfiguration config, IPlatformProxy proxy)
        {
            return new ServiceBundle(config, proxy);
        }

        public void SetPlatformProxyForTest(IPlatformProxy platformProxy)
        {
            PlatformProxy = platformProxy;
        }
    }
}
