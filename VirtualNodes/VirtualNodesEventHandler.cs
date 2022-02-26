using System;
using Umbraco.Core;
using Umbraco.Core.Composing;
#if DEBUG
using Umbraco.Core.Logging;
#endif
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;

namespace VirtualNodes
{
    public class VirtualNodesComponent : IComponent
    {
        private readonly IUmbracoContextFactory _context;
#if DEBUG
        private readonly ILogger _logger;
#endif
        public VirtualNodesComponent(IUmbracoContextFactory context)
        {
            _context        = context;
        }

#if DEBUG
        public VirtualNodesComponent(IUmbracoContextFactory context, ILogger logger) : this(context)
        {
            _logger = logger;
        }
#endif

        public void Initialize()
        {
            ContentService.Saving += (contentService, e) =>
            {
                using (var cref = _context.EnsureUmbracoContext())
                {
                    var cache = cref.UmbracoContext.Content;

                    // Go through nodes being published
                    foreach (IContent node in e.SavedEntities)
                    {
                        // Name of node hasn't changed, so don't do anything
                        if (node.HasIdentity && !node.IsPropertyDirty("Name"))
                        {
                            continue;
                        }

                        IPublishedContent parent;

                        try
                        {
                            // If there is no parent, exit
                            if ((node.ParentId == 0) || (!node.HasIdentity && (node.Level == 1)) || (node.HasIdentity && (node.Level == 0)))
                            {
                                continue;
                            }

                            // Switch to IPublishedContent to go faster
                            parent = cache.GetById(node.ParentId);

                            // If parent is home (redundant) and parent is not a virtual node, exit current iteration
                            if ((parent == null) || (parent.Level < 2) || !parent.IsVirtualNode())
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            _logger.Error(typeof(VirtualNodesComponent), ex);
#endif
                            continue;
                        }

                        // Start the counter. This will count the nodes with the same name (taking numbering under consideration)
                        // that will be found under all the node's parent siblings that are virtual nodes.

                        int nodesFound = 0;
                        int maxNumber = 0;

                        foreach (IPublishedContent farSibling in parent.Siblings())
                        {
                            // Don't take other nodes under considerations - only virtual nodes
                            // I know the name "farSibling" is not that pretty, couldn't think of anything else though.
                            if (!farSibling.IsVirtualNode())
                            {
                                continue;
                            }

                            // For each sibling of the node's parent, get all children and check names
                            foreach (IPublishedContent potentialDuplicate in farSibling.Children())
                            {

                                string p = potentialDuplicate.Name.ToLower();
                                string y = node.Name.ToLower();

                                // Don't take the node itself under consideration - only other nodes.
                                if (potentialDuplicate.Id == node.Id)
                                {
                                    continue;
                                }

                                // If we find a node that already has the same name, increase counter by 1.
                                if (p.Equals(y))
                                {
                                    nodesFound++;
                                }
                                // If we find a node with the same name and numbering immediately after, increase counter by 1.
                                // maxNumber will be the max number we found in node numbering, even if there are deleted node numbers in between.
                                // For example, if we have "aaa (1)" and "aaa(5)" only, maxNumber will be 5.
                                else if (VirtualNodesHelpers.MatchDuplicateName(p, y))
                                {
                                    nodesFound++;
                                    maxNumber = VirtualNodesHelpers.GetMaxNodeNameNumbering(p, y, maxNumber);
                                }
                            }
                        }

                        //Change the node's name to the appropriate number if duplicates were found.
                        //The number of nodes found will be the actual node number since we'll already have a node with
                        //no numbering. Meaning that if there is "aaa", "aaa (1)" and "aaa (2)" then
                        //our new node (initially named "aaa") will be renamed to "aaa (3)" - that is 3 nodes found.
                        if (nodesFound > 0)
                        {
                            node.Name += $" ({maxNumber + 1})";
                        }
                    }
                }
            };

            ContentService.Published += (contentService, e) =>
            {
                Current.AppCaches.RuntimeCache.ClearByKey("CachedVirtualNodes");
            };
        }

        public void Terminate()
        {
            // do nothing
        }
    }
}
