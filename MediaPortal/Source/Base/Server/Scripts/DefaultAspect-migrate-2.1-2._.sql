-- This script is the default for migrating aspect data from database version 2.1 to version 2.*. DO NOT MODIFY!

INSERT INTO %ASPECT_TABLE% SELECT * FROM %ASPECT_TABLE%%SUFFIX%;
INSERT INTO %ASPECT_V_TABLE% SELECT * FROM %ASPECT_V_TABLE%%SUFFIX%;
INSERT INTO %ASPECT_NM_TABLE%(MEDIA_ITEM_ID,VALUE_ID,VALUE_ORDER) SELECT MEDIA_ITEM_ID,VALUE_ID,0 VALUE_ORDER FROM %ASPECT_NM_TABLE%%SUFFIX%;

