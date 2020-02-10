using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Attribute = SC.API.ComInterop.Models.Attribute;

namespace SCQueryConnect.Common.Helpers
{
    public class RelationshipsBuilder : IRelationshipsBuilder
    {
        private readonly ILog _logger;

        public RelationshipsBuilder(ILog logger)
        {
            _logger = logger;
        }

        public async Task AddRelationshipsToStory(Story story, char separator, bool hasRelValue)
        {
            var relAttrib = story.RelationshipAttribute_FindByName("Strength");
            if (hasRelValue && relAttrib == null)
                relAttrib = story.RelationshipAttribute_Add("Strength", RelationshipAttribute.RelationshipAttributeType.Numeric);

            var attribsToTest = new List<Attribute>();

            foreach (var a in story.Attributes)
            {
                if (a.Type == Attribute.AttributeType.Text &&
                    (a.Name.ToLower().Contains("related_") || a.Name == "RelatedItems"))
                {
                    attribsToTest.Add(a);
                }
            }

            if (attribsToTest.Count == 0)
            {
                await _logger.LogWarning("No Related attributes detected.");
                await _logger.LogWarning("Make sure you are using a text attribute called 'Related_<CategoryName>' or just 'RelatedItems'");
                return;
            }

            foreach (var at in attribsToTest)
            {
                bool any = (at.Name == "RelatedItems");
                var catName = at.Name.Substring(8);

                foreach (var i in story.Items)
                {
                    var text = i.GetAttributeValueAsText(at);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var rels = text.Split(separator);
                        foreach (var r in rels)
                        {
                            var Id = r.Trim();
                            var val = r.Substring(r.Length - 1);
                            double num = 0;
                            if (hasRelValue && double.TryParse(val, out num))
                            {
                                num = double.Parse(val);
                                Id = r.Substring(0, Id.Length - 1).Trim();
                            }

                            var i2 = FindItem(story, Id, catName, any);
                            if (i2 != null)
                            {
                                if (story.Relationship_FindByItems(i, i2) == null)
                                {
                                    var rel = story.Relationship_AddNew(i, i2, $"Added from {i.Name}.{at.Name}");
                                    await _logger.Log($"Adding realtionship between '{i.Name}' and '{i2.Name}'");
                                    if (hasRelValue && num > 0)
                                        rel.SetAttributeValue(relAttrib, num);
                                }
                            }
                            else
                            {
                                await _logger.Log($"Could not find item '{Id}' for '{i.Name}.{at.Name}' from 'text'");
                            }
                        }
                    }
                }
            }
        }

        private static Item FindItem(Story story, string extId, string catName, bool any)
        {
            if (any)
                return story.Item_FindByExternalId(extId);

            foreach (var i in story.Items)
            {
                if (i.Category.Name == catName && i.ExternalId == extId)
                    return i;
            }
            return null;// none found
        }
    }
}
