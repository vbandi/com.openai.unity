// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using OpenAI.Models;

namespace OpenAI.Moderations
{
    /// <summary>
    /// Given a input text, outputs if the model classifies it as violating OpenAI's content policy.
    /// </summary>
    public sealed class ModerationsRequest
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="input">
        /// The input text to classify.
        /// </param>
        /// <param name="model">The default is text-moderation-latest which will be automatically upgraded over time.
        /// This ensures you are always using our most accurate model.
        /// If you use text-moderation-stable, we will provide advanced notice before updating the model.
        /// Accuracy of text-moderation-stable may be slightly lower than for text-moderation-latest.
        /// </param>
        [JsonConstructor]
        public ModerationsRequest(string input, Model model = null)
        {
            Input = input;
            Model = model ?? new Model("text-moderation-latest");
        }

        [JsonProperty("input")]
        public string Input { get; }

        [JsonProperty("model")]
        public string Model { get; }
    }
}
