
:: MODIFY THE FOLLOWING COMMANDS AS NEEDED TO MATCH YOUR ENVIRONMENT

SETX ASPNETCORE_ENVIRONMENT "Denis" /M

SETX FOAEA_DB_SERVER "JUPSQLDEV1\FOAEA" /M
SETX FOAEA_API_SERVER "localhost" /M
SETX FOAEA_WEB_SERVER "localhost" /M

SETX FILEBROKER_DB_SERVER "JUPSQLDEV1\FOAEA" /M
SETX FILEBROKER_API_SERVER "localhost" /M
SETX FILEBROKER_WEB_SERVER "localhost" /M

SETX MAIL_SERVER "webmail.justice.gc.ca" /M

:: DO NOT MODIFY THE FOLLOWING SETX COMMANDS

SETX PRODUCTION_WEB_SERVER "OTVFOAEA5" /M 
SETX PRODUCTION_OPS_SERVER "OTVFOAEA2" /M

PAUSE
