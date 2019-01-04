using SC.API.ComInterop.Models;

namespace SCQueryConnect.Helpers
{
    internal class MainWindowHelper
    {
        public string GetStoryUrl(string input)
        {
            if (input.Contains("#/story"))
            {
                var mid = input.Substring(input.IndexOf("#/story") + 8);
                if (mid.Length >= 36)
                {
                    mid = mid.Substring(0, 36);
                    return mid;
                }
            }

            return input;
        }

        public bool Validate(Story story, out string message)
        {
            if (story.Categories.Length == 0)
            {
                message = "Aborting update: story has no categories";
                return false;
            }

            message = $"Reading story '{story.Name}'";
            return true;
        }
    }
}
