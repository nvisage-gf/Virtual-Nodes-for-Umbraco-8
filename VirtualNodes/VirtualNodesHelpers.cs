using System.Text.RegularExpressions;
using Umbraco.Core.Models.PublishedContent;

namespace VirtualNodes
{
    public static class VirtualNodesHelpers
    {
        /// <summary>
        /// Checks a given node's name against a potential duplicate name. If the name is the same, followed by a space, a parenthesis and a number, then this is a duplicate name.
        /// </summary>
        /// <param name="potentialDuplicateName">The name to check against</param>
        /// <param name="currentNodeName">The given node's name</param>
        /// <returns>True if the potential duplicate name is same with the current node's name followed by a parenthesis with a number</returns>
        public static bool MatchDuplicateName(string potentialDuplicateName, string currentNodeName)
        {
            var rgName = new Regex(@"^(.+)( \(\d+\))$");

            return (rgName.IsMatch(potentialDuplicateName) && rgName.Replace(potentialDuplicateName, "$1").Equals(currentNodeName));
        }

        /// <summary>
        /// Gets the largest same-name node number being used
        /// </summary>
        /// <param name="potentialDuplicateName">The name to check for duplicates</param>
        /// <param name="currentNodeName">The current node's name</param>
        /// <param name="maxNumber">The current maximum number</param>
        /// <returns>The new maximum number, if applicable, or the same maximum number if nothing has changed</returns>
        public static int GetMaxNodeNameNumbering(string potentialDuplicateName, string currentNodeName, int maxNumber)
        {
            var rgName = new Regex(@"^.+ \((\d+)\)$");

            if (rgName.IsMatch(potentialDuplicateName))
            {
                var newNumber = int.Parse(rgName.Replace(potentialDuplicateName, "$1"));

                maxNumber = ((maxNumber < newNumber) ? newNumber : maxNumber);
            }

            return maxNumber;
        }

        /// <summary>
        /// Checks if a node is a virtual node
        /// </summary>
        /// <param name="item">The node to check</param>
        /// <returns>True if it is a virtual node</returns>
        public static bool IsVirtualNode(this IPublishedContent item)
        {
            if (item == null) return false;
            
            foreach (string rule in VirtualNodesRuleManager.Instance.Rules)
            {
                if (MatchContentTypeAlias(item.ContentType.Alias, rule))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks rules from settings against a given document type alias to see if it matches the rule
        /// </summary>
        /// <param name="nodeContentTypeAlias">The given document type alias</param>
        /// <param name="contentTypeAliasFromSettings">The rule from settings</param>
        /// <returns>True if it is a match</returns>
        private static bool MatchContentTypeAlias(string nodeContentTypeAlias, string contentTypeAliasFromSettings)
        {
            if (contentTypeAliasFromSettings.EndsWith("*") && contentTypeAliasFromSettings.StartsWith("*"))
            {
                return nodeContentTypeAlias.ToLower().Contains(contentTypeAliasFromSettings.ToLower().Replace("*", ""));
            }
            else if (contentTypeAliasFromSettings.EndsWith("*"))
            {
                return nodeContentTypeAlias.ToLower().StartsWith(contentTypeAliasFromSettings.ToLower().Replace("*", ""));
            }
            else if (contentTypeAliasFromSettings.StartsWith("*"))
            {
                return nodeContentTypeAlias.ToLower().EndsWith(contentTypeAliasFromSettings.ToLower().Replace("*", ""));
            }
            else
            {
                return nodeContentTypeAlias.ToLower().Equals(contentTypeAliasFromSettings.ToLower());
            }
        }
    }
}
