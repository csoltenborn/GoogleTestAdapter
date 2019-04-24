// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GoogleTestAdapter.Common
{
   public class Strings : IStrings
   {
      private static readonly IStrings _strings;

      static Strings()
      {
         _strings = new Strings();

         // this is broken since 0.14.0 for reasons I don't understand. Switching to fixed GTA strings for now...
         // var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GoogleTestAdapter.Common.Dynamic.dll");
         // var asm = Assembly.LoadFile(path);
         // var type = asm.GetType("GoogleTestAdapter.Common.Strings");
         // _strings = (IStrings)Activator.CreateInstance(type);
      }

      public static IStrings Instance => _strings;

      public string ExtensionName => "Google Test Adapter";
      public string TroubleShootingLink => "Check out Google Test Adapter's trouble shooting section at https://github.com/csoltenborn/GoogleTestAdapter#trouble_shooting";
      public string TestDiscoveryStarting => "Google Test Adapter: Test discovery starting...";
      public string TestExecutionStarting => "Google Test Adapter: Test execution starting...";

   }
}