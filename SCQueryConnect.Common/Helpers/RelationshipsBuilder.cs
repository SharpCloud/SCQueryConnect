using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Interfaces;
using System.Threading.Tasks;
using Attribute = SC.API.ComInterop.Models.Attribute;

namespace SCQueryConnect.Common.Helpers
{
    public class RelationshipsBuilder : IRelationshipsBuilder
    {
        private readonly char[] _separators = {';', ','};
        private readonly ILog _logger;

        public RelationshipsBuilder(ILog logger)
        {
            _logger = logger;
        }

        public async Task AddRelationshipsToStory(Story story)
        {
            Attribute relatedItemAttribute = null;

            foreach (var a in story.Attributes)
            {
                if ((a.Type == Attribute.AttributeType.Text ||
                     a.Type == Attribute.AttributeType.List) &&
                    a.Name == "RelatedItems")
                {
                    relatedItemAttribute = a;
                    break;
                }
            }

            if (relatedItemAttribute == null)
            {
                await _logger.LogWarning("Warning: Could not find a 'RelatedItems' attribute.");
                await _logger.Log("Please make sure you have a Text or List attribute called 'RelatedItems' in this story.");
                await _logger.Log("Exiting process.");
                return;
            }

            int countAny = 0;
            int countNew = 0;

            foreach (var i in story.Items)
            {
                var text = i.GetAttributeValueAsText(relatedItemAttribute);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    countAny++;
                    var rels = text.Split(_separators);
                    foreach (var r in rels)
                    {
                        var Id = r.Trim();
                        if (!string.IsNullOrWhiteSpace(Id))
                        {
                            var i2 = story.Item_FindByExternalId(Id);
                            if (i2 != null)
                            {
                                if (story.Relationship_FindByItems(i, i2) == null)
                                {
                                    var rel = story.Relationship_AddNew(i, i2);
                                    countNew++;
                                    await _logger.Log($"Adding relationship between '{i.Name}' and '{i2.Name}'");
                                }
                            }
                            else
                            {
                                await _logger.Log(
                                    $"Could not find related item with '{Id}' for item '{i.Name}' from '{text}'");
                            }
                        }
                    }
                }
            }

            if (countAny == 0)
            {
                await _logger.Log($"No items with RelatedItems text fields found.");
            }
            else
            {
                await _logger.Log($"{countAny} records processed, {countNew} new relationships added.");
            }
        }
    }
}
