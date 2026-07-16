import requests
import time
import json
import os

# ==============================================================================
# CONFIGURATION
# ==============================================================================
# You will need to obtain these by creating an app in the LinkedIn Developer Portal
# and generating an OAuth 2.0 access token with the `w_member_social` permission.

ACCESS_TOKEN = os.environ.get("LINKEDIN_ACCESS_TOKEN", "YOUR_ACCESS_TOKEN_HERE")
# Your URN usually looks like: urn:li:person:12345ABCDE
AUTHOR_URN = os.environ.get("LINKEDIN_AUTHOR_URN", "urn:li:person:YOUR_ID_HERE")

# The URL where your markdown article is hosted (e.g., a Medium post, GitHub gist, or personal blog)
ARTICLE_URL = "https://your-blog.com/path-to-article"

# The catchy intro text for the LinkedIn feed
POST_TEXT = """Are your complex multi-table SQL JOINs bringing your directory database to its knees? 📉 

When building the search engine for BizSort, we hit the classic N+1 filtering nightmare: thousands of users cross-referencing categories, locations, and exclusions simultaneously. 

Our solution? We completely decoupled the read-path from the write-path using a two-way synchronization loop, generic facets, and background indexers. We traded a slight delay on profile updates for O(1), sub-millisecond search query times.

I just wrote a deep dive on how we architected this dynamic Materialized View (and why we explicitly chose it over a traditional Star Schema). 

Read the full breakdown below! 👇
#SoftwareEngineering #Architecture #DatabaseOptimization #DotNet #EFCore #TechScalability"""

# ==============================================================================

def publish_linkedin_post():
    """Publishes a post to LinkedIn using the v2/posts API."""
    print("Attempting to publish post to LinkedIn...")
    
    url = "https://api.linkedin.com/v2/ugcPosts"
    
    headers = {
        "Authorization": f"Bearer {ACCESS_TOKEN}",
        "X-Restli-Protocol-Version": "2.0.0",
        "Content-Type": "application/json"
    }

    payload = {
        "author": AUTHOR_URN,
        "lifecycleState": "PUBLISHED",
        "specificContent": {
            "com.linkedin.ugc.ShareContent": {
                "shareCommentary": {
                    "text": POST_TEXT
                },
                "shareMediaCategory": "ARTICLE",
                "media": [
                    {
                        "status": "READY",
                        "description": {
                            "text": "Implementing Efficient Facet-Based Search"
                        },
                        "originalUrl": ARTICLE_URL,
                        "title": {
                            "text": "Implementing Efficient Facet-Based Search and Navigation for a Directory"
                        }
                    }
                ]
            }
        },
        "visibility": {
            "com.linkedin.ugc.MemberNetworkVisibility": "PUBLIC"
        }
    }

    response = requests.post(url, headers=headers, data=json.dumps(payload))

    if response.status_code == 201:
        print(f"Success! Post published. Post URN: {response.json().get('id')}")
    else:
        print(f"Failed to publish post. Status Code: {response.status_code}")
        print(f"Error Message: {response.text}")


def schedule_post(hours_from_now):
    """Waits for the specified number of hours before publishing."""
    seconds_to_wait = hours_from_now * 3600
    
    print(f"Post scheduled for {hours_from_now} hours from now.")
    print(f"Do not close this script. It will sleep for {seconds_to_wait} seconds...")
    
    try:
        time.sleep(seconds_to_wait)
        publish_linkedin_post()
    except KeyboardInterrupt:
        print("\nScheduling cancelled by user.")

if __name__ == "__main__":
    # Schedule the post for 24 hours from now
    schedule_post(24)
