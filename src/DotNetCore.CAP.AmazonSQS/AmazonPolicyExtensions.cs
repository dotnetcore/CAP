using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;

namespace DotNetCore.CAP.AmazonSQS
{
    public static class AmazonPolicyExtensions
    {
        /// <summary>
        /// Check to see if the policy for the queue has already given permission to the topic.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="topicArn"></param>
        /// <param name="sqsQueueArn"></param>
        /// <returns></returns>
        public static bool HasSqsPermission(this Policy policy, string topicArn, string sqsQueueArn)
        {
            foreach (var statement in policy.Statements)
            {
                var containsResource = statement.Resources.Any(r => r.Id.Equals(sqsQueueArn));

                if (!containsResource)
                {
                    continue;
                }

                foreach (var condition in statement.Conditions)
                {
                    if ((string.Equals(condition.Type, ConditionFactory.StringComparisonType.StringLike.ToString(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(condition.Type, ConditionFactory.StringComparisonType.StringEquals.ToString(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(condition.Type, ConditionFactory.ArnComparisonType.ArnEquals.ToString(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(condition.Type, ConditionFactory.ArnComparisonType.ArnLike.ToString(), StringComparison.OrdinalIgnoreCase)) &&
                        string.Equals(condition.ConditionKey, ConditionFactory.SOURCE_ARN_CONDITION_KEY, StringComparison.OrdinalIgnoreCase) &&
                        condition.Values.Contains(topicArn))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Add statement to the SQS policy that gives the SNS topics access to send a message to the queue.
        /// </summary>
        /// <code>
        /// {
        ///     "Version": "2012-10-17",
        ///     "Statement": [
        ///     {
        ///         "Effect": "Allow",
        ///         "Principal": {
        ///             "AWS": "*"
        ///         },
        ///         "Action": "sqs:SendMessage",
        ///         "Resource": "arn:aws:sqs:us-east-1:MyQueue",
        ///         "Condition": {
        ///             "ArnLike": {
        ///                 "aws:SourceArn": [
        ///                 "arn:aws:sns:us-east-1:FirstTopic",
        ///                 "arn:aws:sns:us-east-1:SecondTopic"
        ///                     ]
        ///             }
        ///         }
        ///     }]
        /// }
        /// </code>
        /// <param name="policy"></param>
        /// <param name="topicArns"></param>
        /// <param name="sqsQueueArn"></param>
        public static void AddSqsPermissions(this Policy policy, IEnumerable<string> topicArns, string sqsQueueArn)
        {
            var statement = new Statement(Statement.StatementEffect.Allow);
#pragma warning disable CS0618 // Type or member is obsolete
            statement.Actions.Add(SQSActionIdentifiers.SendMessage);
#pragma warning restore CS0618 // Type or member is obsolete
            statement.Resources.Add(new Resource(sqsQueueArn));
            statement.Principals.Add(new Principal("*"));
            foreach (var topicArn in topicArns)
            {
                statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(topicArn));
            }

            policy.Statements.Add(statement);
        }

        /// <summary>
        /// Compact SQS access policy
        /// </summary>
        /// <para>
        /// Transforms policies with multiple similar statements:
        /// <code>
        /// {
        ///     "Version": "2012-10-17",
        ///     "Statement": [
        ///     {
        ///         "Effect": "Allow",
        ///         "Principal": {
        ///             "AWS": "*"
        ///         },
        ///         "Action": "sqs:SendMessage",
        ///         "Resource": "arn:aws:sqs:us-east-1:MyQueue-v1",
        ///         "Condition": {
        ///             "ArnLike": {
        ///                 "aws:SourceArn": "arn:aws:sns:us-east-1:MyQueue-FirstTopic"
        ///             }
        ///         }
        ///     },
        ///     {
        ///         "Effect": "Allow",
        ///         "Principal": {
        ///             "AWS": "*"
        ///         },
        ///         "Action": "sqs:SendMessage",
        ///         "Resource": "arn:aws:sqs:us-east-1:MyQueue-v1",
        ///         "Condition": {
        ///             "ArnLike": {
        ///                 "aws:SourceArn": "arn:aws:sns:us-east-1:MyQueue-SecondTopic"
        ///             }
        ///         }
        ///     },
        ///     {
        ///         "Effect": "Allow",
        ///         "Principal": {
        ///             "AWS": "*"
        ///         },
        ///         "Action": "sqs:SendMessage",
        ///         "Resource": "arn:aws:sqs:us-east-1:MyQueue-v1",
        ///         "Condition": {
        ///             "ArnLike": {
        ///                 "aws:SourceArn": "arn:aws:sns:us-east-1:MyQueue2-FirstTopic"
        ///             }
        ///         }
        ///     },]
        /// }
        /// </code>
        ///     into compacted single statement:
        /// <code>
        /// {
        ///     "Version": "2012-10-17",
        ///     "Statement": [
        ///     {
        ///         "Effect": "Allow",
        ///         "Principal": {
        ///             "AWS": "*"
        ///         },
        ///         "Action": "sqs:SendMessage",
        ///         "Resource": "arn:aws:sqs:us-east-1:MyQueue-v1",
        ///         "Condition": {
        ///             "ArnLike": {
        ///                 "aws:SourceArn": [
        ///                     "arn:aws:sns:us-east-1:MyQueue-*",
        ///                     "arn:aws:sns:us-east-1:MyQueue2-FirstTopic"
        ///                 ]
        ///             }
        ///         }
        ///     }]
        /// }
        /// </code>
        /// </para>
        /// <param name="policy"></param>
        /// <param name="sqsQueueArn"></param>
        public static void CompactSqsPermissions(this Policy policy, string sqsQueueArn)
        {
            var statementsToCompact = policy.Statements
                .Where(s => s.Effect == Statement.StatementEffect.Allow)
#pragma warning disable CS0618 // Type or member is obsolete
                .Where(s => s.Actions.All(a => string.Equals(a.ActionName, SQSActionIdentifiers.SendMessage.ActionName, StringComparison.OrdinalIgnoreCase)))
#pragma warning restore CS0618 // Type or member is obsolete
                .Where(s => s.Resources.All(r => string.Equals(r.Id, sqsQueueArn, StringComparison.OrdinalIgnoreCase)))
                .Where(s => s.Principals.All(r => string.Equals(r.Id, "*", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var groupName = GetGroupName(sqsQueueArn);
            if (groupName != null)
            {
                groupName = $":{groupName}-";
            }
            if (statementsToCompact.Count < 2 && groupName == null)
            {
                return;
            }

            var topicArns = new HashSet<string>();
            foreach (var statement in statementsToCompact)
            {
                policy.Statements.Remove(statement);
                foreach (var topicArn in statement.Conditions.SelectMany(c => c.Values))
                {
                    topicArns.Add(
                        groupName != null && topicArn.Contains(groupName, StringComparison.InvariantCultureIgnoreCase)
                            ? $"{GetArnGroupPrefix(topicArn)}-*"
                            : topicArn);
                }
            }

            policy.AddSqsPermissions(topicArns.OrderBy(a => a), sqsQueueArn);
        }

        /// <summary>
        /// Extract group prefix from ARN
        /// For example for ARN:
        ///     arn:aws:sns:us-east-1:MyQueue-FirstTopic
        /// group prefix will be extracted:
        ///     arn:aws:sns:us-east-1:MyQueue
        /// </summary>
        /// <param name="arn">Source ARN</param>
        /// <returns>Group prefix or null if group not present</returns>
        private static string? GetArnGroupPrefix(string arn)
        {
            const char separator = '-';
            if (string.IsNullOrEmpty(arn) || !arn.Contains(separator))
            {
                return null;
            }

            var groupPaths = arn.Split(separator);
            if (groupPaths.Length < 2)
            {
                return null;
            }

            return string.Join(separator, groupPaths.Take(groupPaths.Length - 1));
        }
        
        /// <summary>
        /// Extract group name from ARN
        /// For example for ARN:
        ///     arn:aws:sns:us-east-1:MyQueue-FirstTopic
        /// group name will be extracted:
        ///     MyQueue
        /// </summary>
        /// <param name="arn">Source ARN</param>
        /// <returns>Group name or null if group not present</returns>
        private static string? GetGroupName(string arn)
        {
            const char separator = ':';
            if (string.IsNullOrEmpty(arn) || !arn.Contains(separator))
            {
                return null;
            }

            var name = arn.Split(separator).LastOrDefault();
            if(string.IsNullOrEmpty(name))
            {
                return null;
            }

            return GetArnGroupPrefix(name);
        }
    }
}