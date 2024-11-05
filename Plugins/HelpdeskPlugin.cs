using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Plugins
{

    public sealed class HelpdeskPlugin
    {

        [KernelFunction, Description("Reset a user's password.")]
        public void ResetPassword(
            [Description("The user id.")] string serialNumber)
        {
            Console.WriteLine($"Resetting password for user {serialNumber}");
        }
    }
}