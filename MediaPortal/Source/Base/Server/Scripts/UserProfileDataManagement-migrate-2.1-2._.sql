-- This script migrates UserProfileDataManagement schema data from database version 2.1 to version 2.*. DO NOT MODIFY!

INSERT INTO USER_PROFILES(PROFILE_ID,NAME,PROFILE_TYPE) SELECT PROFILE_ID,NAME,0 PROFILE_TYPE FROM USER_PROFILES%SUFFIX%;
INSERT INTO USER_MEDIA_ITEM_DATA SELECT * FROM USER_MEDIA_ITEM_DATA%SUFFIX%;
INSERT INTO USER_ADDITIONAL_DATA(PROFILE_ID,DATA_KEY,DATA_NO,ADDITIONAL_DATA) SELECT PROFILE_ID,DATA_KEY,0 DATA_NO,ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA%SUFFIX%;
INSERT INTO USER_PLAYLIST_DATA SELECT * FROM USER_PLAYLIST_DATA%SUFFIX%;

