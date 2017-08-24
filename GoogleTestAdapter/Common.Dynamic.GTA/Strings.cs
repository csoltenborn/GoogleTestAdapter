// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GoogleTestAdapter.Common
{
    public class Strings : IStrings
    {
        public string ExtensionName => "Google Test Adapter";
        public string TroubleShootingLink => "{0}Check out Google Test Adapter's trouble shooting section at https://github.com/csoltenborn/GoogleTestAdapter#trouble_shooting";
        public string TestDiscoveryStarting => "Google Test Adapter: Test discovery starting...";
        public string TestExecutionStarting => "Google Test Adapter: Test execution starting...";
    }
}
