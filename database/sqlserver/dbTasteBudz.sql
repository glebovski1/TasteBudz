USE dbSQL1;     -- Get out of the master database
SET NOCOUNT ON; -- Report only errors

CREATE TABLE TUsers 
(
	intUserID			INTEGER	IDENTITY	NOT NULL
	,strFirstname		VARCHAR(255)		NOT NULL
	,strLastName		VARCHAR(255)		NOT NULL
	,strUserName		VARCHAR(255)		NOT NULL
	,strEmail			VARCHAR(255)		NOT NULL
	,strPassword		VARCHAR(255)		NOT NULL
	,intZipcode			INTEGER				NOT NULL
	,strBio				VARCHAR(255)		NOT NULL
	,intSpiceLevel		INTEGER				NOT NULL
	,intGroupSizeID		INTEGER				NOT NULL
	,intRomanceChoiceID	INTEGER				NOT NULL
	CONSTRAINT TUsers_PK PRIMARY KEY(intUserID)
)

CREATE TABLE TGroupSizes
(
	intGroupSizeID		INTEGER	IDENTITY	NOT NULL
	,strGroupSize		VARCHAR(255)		NOT NULL
	CONSTRAINT TGroupSize_PK PRIMARY KEY(intGroupSizeID)
)

CREATE TABLE TRomanceChoices
(
	intRomanceChoiceID	INTEGER	IDENTITY	NOT NULL
	,strRomanceChoice	VARCHAR(255)		NOT NULL
	CONSTRAINT TRomanceChoices_PK PRIMARY KEY(intRomanceChoiceID)
)

CREATE TABLE TCuisines
(
	intCuisineID		INTEGER	IDENTITY	NOT NULL
	,strCuisine			VARCHAR(255)		NOT NULL
	CONSTRAINT TCuisine_PK PRIMARY KEY(intCuisineID)
)

CREATE TABLE TUserCuisines
(
	intUserCuisineID	INTEGER	IDENTITY	NOT NULL
	,intUserID			INTEGER				NOT NULL
	,intCuisineID		INTEGER				NOT	NULL
	CONSTRAINT TUserCuisine_PK PRIMARY KEY(intUserCuisineID)
)

CREATE TABLE TAllergies
(
	intAllergyID		INTEGER	IDENTITY	NOT NULL
	,strAllergy			INTEGER				NOT NULL
	CONSTRAINT TAllergies_PK PRIMARY KEY(intAllergyID)
)

CREATE TABLE TUserAllergies
(
	intUserAllergyID	INTEGER	IDENTITY	NOT NULL
	,intUserID			INTEGER				NOT NULL
	,intAllergyID		INTEGER				NOT NULL
	CONSTRAINT TUserAllergies_PK PRIMARY KEY(intAllergyID)
)

CREATE TABLE TDietaryNeeds
(
	intDietaryNeedID		INTEGER	IDENTITY	NOT NULL
	,strDietaryNeed			VARCHAR(255)		NOT NULL
	CONSTRAINT TDietaryNeeds PRIMARY KEY(intDietaryNeedID)
)

CREATE TABLE TUserDietaryNeeds
(
	intUserDietaryNeedID	INTEGER	IDENTITY	NOT NULL
	,intUserID				INTEGER				NOT NULL
	,intDietaryNeedID		INTEGER				NOT NULL
	CONSTRAINT TUserDietaryNeeds PRIMARY KEY(intUserDietaryNeedID)
)

CREATE TABLE TAchievements
(
	intAchievementID	INTEGER	IDENTITY	NOT NULL
	,strName			VARCHAR(255)		NOT NULL
	,strDescription		VARCHAR(255)		NOT NULL
	CONSTRAINT TAchievements_PK PRIMARY KEY(intAchievementID)
)

CREATE TABLE TUserAchievements
(
	intUserAchievementID	INTEGER IDENTITY	NOT NULL
	,intUserID				INTEGER				NOT NULL
	,intAchievementID		INTEGER				NOT NULL
	CONSTRAINT	TUserAchievements_PK PRIMARY KEY(intUserAchievementID)
)

CREATE TABLE TBudz		
-- This is how we associate friends, or "Budz" Still a many to many
-- relationship since users can have many users, for lack of better
-- phrasing
(												
	intBudID		INTEGER	IDENTITY	NOT NULL		
	,intUserOneID	INTEGER				NOT NULL
	,intUserTwoID	INTEGER				NOT NULL
	CONSTRAINT TBudz_PK PRIMARY KEY(intBudID)
)

CREATE TABLE TBlocks
-- Like tBudz, but the complete opposite. Similar table structure, but different function
(
	intBlockID			INTEGER	IDENTITY	NOT NULL
	,intBlockerUserID	INTEGER				NOT NULL -- The user who is blocking someone else
	,intBlockedUserID	INTEGER				NOT NULL -- The user who is being blocked
	CONSTRAINT TBlocks_PK PRIMARY KEY (intBlockID)
)

--TChats and TMessages are up here since the Chat PK is a FK in both Groups and Events
CREATE TABLE TChats
(
	intChatID		INTEGER	IDENTITY	NOT NULL
	CONSTRAINT TChats_PK PRIMARY KEY(intChatID)
)

CREATE TABLE TMessage
(
	intMessageID		INTEGER	IDENTITY	NOT NULL
	,intSenderID		INTEGER				NOT NULL
	,strMessage			VARCHAR(160)		NOT NULL
	,dtDateSent			DATETIME			NOT NULL
	,intRecieverID		INTEGER				NOT NULL
	,intChatID			INTEGER				NOT NULL
	CONSTRAINT TMessages_PK PRIMARY KEY(intMessageID)
)


CREATE TABLE TGroups
(
	intGroupID				INTEGER	IDENTITY	NOT NULL
	,intUserID				INTEGER				NOT NULL --This will be the creator of the group ONLY 
	,strGroupName			VARCHAR(255)		NOT NULL
	,intCuisineID			INTEGER				NOT NULL
	,strGroupDescription	VARCHAR(255)		NOT NULL
	,intMaxGroupSize		VARCHAR(255)		NOT NULL
	,intPrivacyCoiceID		VARCHAR(255)		NOT NULL
	,intChatID				INTEGER				NOT NULL
	CONSTRAINT TGroups_PK PRIMARY KEY(intGroupID)
)

CREATE TABLE TPrivacyChoices
(
	intPrivacyChoiceID		INTEGER	IDENTITY	NOT NULL
	,strPrivacyChoice		INTEGER				NOT NULL				-- 1 = Open enrollment to public
	CONSTRAINT TPrivacyChoices_PK PRIMARY KEY(intPrivacyChoiceID)	-- 2 = Only organizer can invite people
)

CREATE TABLE TGroupUsers
(
	intGroupUserID			INTEGER	IDENTITY	NOT NULL
	,intGroupID				INTEGER				NOT NULL
	,intUserID				INTEGER				NOT NULL
	CONSTRAINT TGroupUsers_PK PRIMARY KEY(intGroupUserID)
)

CREATE TABLE TRestaurants
(
	intRestaurantID		INTEGER IDENTITY	NOT NULL
	,strRestaurantName	VARCHAR(255)		NOT NULL
	,strAddress			VARCHAR(255)		NOT NULL
)

CREATE TABLE TRestaurantsCuisines
(
	intRestaurantCuisineID	INTEGER IDENTITY	NOT NULL
	,intRestaurantID		INTEGER				NOT NULL
	,intCuisinesID			INTEGER				NOT NULL
)

CREATE TABLE TEvents
(
	intEventID			INTEGER	IDENTITY	NOT NULL
	,strEventName		VARCHAR(255)		NOT NULL
	,intEventTypeID		INTEGER				NOT NULL
	,dEventDate			DATE				NOT NULL
	,tEventTime			TIME				NOT NULL
	,intRestaurantID	INTEGER				NOT NULL
	,intChatID			INTEGER				NOT NULL
	CONSTRAINT TEvents_PK PRIMARY KEY(intEventID)
)

CREATE TABLE TEventTypes
(
	intEventTypeID		INTEGER	IDENTITY	NOT NULL
	,strEventType		VARCHAR(255)		NOT NULL
	CONSTRAINT TEventTypes_PK PRIMARY KEY(intEventTypeID)
)

CREATE TABLE TEventsUsers
(
	intEventUserID		INTEGER	IDENTITY	NOT NULL
	,intEventID			INTEGER				NOT NULL
	,intUserID			INTEGER				NOT NULL
	CONSTRAINT TEventsUsers_PK PRIMARY KEY(intEventUserID)
)

-- ------------------------------------------------------------------------------
-- Step #2: Creating the FK Table
-- ------------------------------------------------------------------------------
--	No.	Child							Parent						Foreign Key
---------------------------------------------------------------------------------
--  1)  TUsers							TGroupSizes					intGroupSizeID
--  2)  TUsers							TRomanceChoices				intRomanceChoice
--  3)	TUserCuisines					TUsers						intUserID
--	4)	TUserCuisines					TCuisines					intCuisineID
--	5)	TUserAllergies					TUsers						intUserID
--	6)	TUserAllergies					TAllergies					intAllergyID
--	7)	TUserDietaryNeeds				TUsers						intUserID
--	8)	TUserDietaryNeeds				TDietaryNeeds				intDietaryNeedID
--	9)	TUserAchievements				TUsers						intUserID
-- 10)	TUserAchievements				TAchievements				intAchievementID
-- 11)	TBudz							TUsers						intUserOneID
-- 12)	TBudz							TUsers						intUserTwoID
-- 13)	TBlocks							TUsers						intBlockerID
-- 14)	TBlocks							TUsers						intBlockedID
-- 15)	TMessages						TChats						intChatID
-- 16)	TGroups							TChats						intChatID
-- 17)	TGroups							TPrivacyChoices				intPrivacyChoiceID
-- 18)	TGroupUsers						TGroups						intGroupID
-- 19)	TGroupUsers						TUsers						intUserID
-- 20)	TRestaurantsCuisines			TRestaurants				intRestaurantID
-- 21)	TRestaurantsCuisines			TCuisines					intCuisineID	
-- 22)	TEvents							TChats						intChatID
-- 23)	TEvents							TEventTypes					intEventTypeID
-- 24)	TEvents							TRestaurants				intRestaurantID
-- 25)	TEventsUsers					TEvents						intEventID
-- 26)	TEventsUsers					TUsers						intUserID

-- 1)
ALTER TABLE TUsers ADD CONSTRAINT TUsers_TGroupSizes_FK
FOREIGN KEY (intGroupSizeID) REFERENCES TGroupSizes (intGroupSizeID)

-- 2)
ALTER TABLE TUsers ADD CONSTRAINT TUsers_TRomanceChoices_FK
FOREIGN KEY (intRomanceChoiceID) REFERENCES TRomanceChoices (intRomanceChoiceID)

-- 3)
ALTER TABLE TUserCuisines ADD CONSTRAINT TUserCuisines_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)

-- 4)
ALTER TABLE TUserCuisines ADD CONSTRAINT TUserCuisines_TCuisines_FK
FOREIGN KEY (intCuisineID) REFERENCES TCuisines (intCuisineID)

-- 5)
ALTER TABLE TUserAllergies ADD CONSTRAINT TUserAllergies_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)

-- 6)
ALTER TABLE TUserAllergies ADD CONSTRAINT TUserAllergies_TAllergies_FK
FOREIGN KEY (intAllergyID) REFERENCES TAllergies (intAllergyID)

-- 7)
ALTER TABLE TUserDietaryNeeds ADD CONSTRAINT TUserDietaryNeeds_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)

-- 8)
ALTER TABLE TUserDietaryNeeds ADD CONSTRAINT TUserDietaryNeeds_TDietaryNeeds_FK
FOREIGN KEY (intDietaryNeedID) REFERENCES TDietaryNeeds (intDietaryNeedID)

-- 9)
ALTER TABLE TUserAchievements ADD CONSTRAINT TUserAchievements_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)

-- 10)
ALTER TABLE TUserAchievements ADD CONSTRAINT TUserAchievements_TAchievements_FK
FOREIGN KEY (intAchievementID) REFERENCES TUsers (intAchievementID)

-- 11)
ALTER TABLE TBudz ADD CONSTRAINT TBudz_TUsers_FK
FOREIGN KEY (intUserOneID) REFERENCES TUsers (intUserID)

-- 12)
ALTER TABLE TBudz ADD CONSTRAINT TBudz_TUsers_FK
FOREIGN KEY (intUserTwoID) REFERENCES TUsers (intUserID)

-- 13)
ALTER TABLE TBlocks ADD CONSTRAINT TBlocks_TUsers_FK
FOREIGN KEY (intBlockerID) REFERENCES TUsers (intUserID)

-- 14)
ALTER TABLE TBlocks ADD CONSTRAINT TBlocks_TUsers_FK
FOREIGN KEY (intBlockedID) REFERENCES TUsers (intUserID)

-- 15)
ALTER TABLE TMessages ADD CONSTRAINT TMessages_TChats_FK
FOREIGN KEY (intChatID) REFERENCES TChats (intChatID)

-- 16)
ALTER TABLE TGroups ADD CONSTRAINT TGroups_TChats_FK
FOREIGN KEY (intChatID) REFERENCES TChats (intChatID)

-- 17)
ALTER TABLE TGroups ADD CONSTRAINT TGroups_TPrivacyChoices_FK
FOREIGN KEY (intPrivacyChoiceID) REFERENCES TChats (intPrivacyChoiceID)

-- 18)
ALTER TABLE TGroupsUsers ADD CONSTRAINT TGroupsUsers_TGroups_FK
FOREIGN KEY (intGroupID) REFERENCES TGroups (intGroupID)

-- 19)
ALTER TABLE TGroupsUsers ADD CONSTRAINT TGroupsUsers_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)

-- 20)
ALTER TABLE TRestaurantsCuisines ADD CONSTRAINT TRestaurantsCuisines_TRestaurants_FK
FOREIGN KEY (intRestaurantID) REFERENCES TRestaurants (intRestaurantID)

-- 21)
ALTER TABLE TRestaurantsCuisines ADD CONSTRAINT TRestaurantsCuisines_TCuisines_FK
FOREIGN KEY (intCuisineID) REFERENCES TCuisines (intCuisineID)

-- 22)
ALTER TABLE TEvents ADD CONSTRAINT TEvents_TChats_FK
FOREIGN KEY (intChatID) REFERENCES TChats (intChatID)

-- 23)
ALTER TABLE	TEvent ADD CONSTRAINT TEvent_TEventTypes_FK
FOREIGN KEY (intEventTypeID) REFERENCES TEventTypes (intEventTypeID)

-- 24)
ALTER TABLE TEvents ADD CONSTRAINT TEvents_TRestaurants_FK
FOREIGN KEY (intRestaurantID) REFERENCES TRestaurants (intRestaurantID)

-- 25)
ALTER TABLE TEventsUsers ADD CONSTRAINT TEventsUsers_TEvents_FK
FOREIGN KEY (intEventID) REFERENCES TEvents (intEventID)

-- 26)
ALTER TABLE TEventsUsers ADD CONSTRAINT TEventsUsers_TUsers_FK
FOREIGN KEY (intUserID) REFERENCES TUsers (intUserID)