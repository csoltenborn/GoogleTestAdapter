using System;

namespace GoogleTestAdapter.Common
{
    /// <summary>
    /// Static class to hold auxiliary functions related to functional programming.
    /// </summary>
    public static class Functional
    {
        /// <summary>
        /// Auxiliary function needed if you want to assign a lambda expression with an anonymous (return) type to a variable (of type Func).
        /// </summary>
        /// <example><code>
        /// var myLambda = Functional.GetFunc((int i) => new { in = i, mod2 = i%2 });
        /// </code></example>
        /// <typeparam name="TArgument">Type of the lambda-function's parameter.</typeparam>
        /// <typeparam name="TAnonymousReturnType">Anonymous type of the lambda-function's return value.</typeparam>
        /// <param name="lambda">Lambda function / expression with a single argument</param>
        /// <returns>Func that can by assigned to a local variable.</returns>
        public static Func<TArgument, TAnonymousReturnType> ToFunc<TArgument, TAnonymousReturnType>(Func<TArgument, TAnonymousReturnType> lambda)
        {
            return lambda;
        }
    }
}
