using Newtonsoft.Json;

namespace InstaSharperDirect.Classes.ResponseWrappers
{
    /*
    {
	    "logged_in_user": {
		    "can_see_organic_insights": false,
		    "account_type": 1,
		    "profile_pic_url": "https://instagram.flhr3-1.fna.fbcdn.net/vp/2bf983cc40290da9e3649b535dac2df9/5C81A77A/t51.2885-19/11906329_960233084022564_1448528159_a.jpg",
		    "nametag": {
			    "mode": 0,
			    "gradient": "0",
			    "emoji": "\ud83d\ude00",
			    "selfie_sticker": "0"
		    },
		    "has_placed_orders": false,
		    "show_insights_terms": false,
		    "onboarding_configurations": [{
				    "type": 3,
				    "name": "Media Creator",
				    "description": "Best for public figures, publishers, content producers, artists and influencers",
				    "verticalID": 1836018136453734,
				    "valueProps": [{
						    "type": 1,
						    "name": "Get Insights",
						    "description": "Learn how your posts and videos are performing."
					    }
				    ]
			    }, {
				    "type": 2,
				    "name": "Business",
				    "description": "Best for retailers, local businesses, brands, organizations and service providers",
				    "verticalID": 1836018136453734,
				    "valueProps": [{
						    "type": 1,
						    "name": "Get Insights",
						    "description": "Learn how your posts and videos are performing."
					    }
				    ]
			    }
		    ],
		    "can_boost_post": false,
		    "is_business": false,
		    "is_call_to_action_enabled": null,
		    "reel_auto_archive": "unset",
		    "is_private": false,
		    "full_name": "mikeadv08",
		    "pk": 7315612197,
		    "username": "mikeadv08",
		    "is_verified": false,
		    "allowed_commenter_type": "any",
		    "has_anonymous_profile_picture": true,
		    "allow_contacts_sync": true,
		    "phone_number": ""
	    },
	    "action": "close",
	    "auto_login": true,
	    "status": "ok"
    }

    */
    public class SendChallengeCodeResponse
    {
        [JsonProperty("logged_in_user")]
        public SendChallengeCodeResponseLoggedInUser LoggedInUser { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("auto_login")]
        public string AutoLogin { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class SendChallengeCodeResponseLoggedInUser
    {
        [JsonProperty("profile_pic_url")]
        public string ProfilePicUrl { get; set; }

        [JsonProperty("is_business")]
        public bool IsBusiness { get; set; }

        [JsonProperty("is_private")]
        public bool IsPrivate { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("pk")]
        public long Pk { get; set; }
        
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }


        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }
    }
}
