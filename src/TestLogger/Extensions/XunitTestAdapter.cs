// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spekt.TestLogger.Extensions
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Spekt.TestLogger.Core;

    public class XunitTestAdapter : ITestAdapter
    {
        public List<TestResultInfo> TransformResults(
            List<TestResultInfo> results,
            List<TestMessageInfo> messages)
        {
            var transformedResults = new List<TestResultInfo>();

            // Process all the messages collected during the test run
            // If one ends with [SKIP], then the next message contains the skip reason.
            var skippedTestNamesWithReason = new Queue<(string testName, string skipReason)>();
            for (int i = 0; i < messages.Count; i++)
            {
                string message = messages[i].Message;
                if (!message.EndsWith("[SKIP]"))
                {
                    continue;
                }

                // remove the gunk ...
                int from = message.IndexOf("]") + 1;
                int to = message.LastIndexOf("[") - from;
                string testName = message.Substring(from, to).Trim();

                string reasonMessage = messages[++i].Message;
                from = reasonMessage.IndexOf("]") + 1;
                string reason = reasonMessage.Substring(from).Trim();

                skippedTestNamesWithReason.Enqueue((testName, reason));
            }

            foreach (var result in results)
            {
                if (result.Outcome == TestOutcome.Skipped)
                {
                    var skipReason = GetSkipReason(result.TestResultDisplayName);

                    // TODO: Defining a new category for now...
                    result.Messages.Add(new TestResultMessage("skipReason", skipReason));
                }

                string displayName = result.TestResultDisplayName;

                // Add parameters for theories.
                if (string.IsNullOrWhiteSpace(displayName) == false &&
                    displayName.IndexOf("(") is int i &&
                    i > 0)
                {
                    result.Method += displayName.Substring(i);
                }

                transformedResults.Add(result);
            }

            return transformedResults;

            string GetSkipReason(string expectedTestName)
            {
                // assume the call order matches the order of the items in the queue
                if (skippedTestNamesWithReason.Count > 0)
                {
                    var (testName, knownSkipReason) = skippedTestNamesWithReason.Dequeue();
                    if (testName == expectedTestName)
                    {
                        return knownSkipReason;
                    }
                }

                const string unknownSkipReason = "N/A";
                return unknownSkipReason;
            }
        }
    }
}