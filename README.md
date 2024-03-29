# foaea-laeoef-r
## FOAEA Re-engineering / AEOEF Réingénierie
### Setup
In order to be able to run FOAEA, you need to set up a number of environment variables.
You can do so by going into the Windows interface for System Environment variables, or by creating and running a batch (.cmd) file that will set them up. This file needs to run with local administrator privileges. 

The following variables need to be set (this example shows the code to run from command line or batch file running as a local administrator -- *the* /M *specifies to set the variable in the system environment since the default setting is the local user's environment*):

    SETX ASPNETCORE_ENVIRONMENT "{your first name}" /M

    SETX FOAEA_DB_SERVER "{foaea database server name}" /M
    SETX FOAEA_API_SERVER "{foaea api server name}" /M
    SETX FOAEA_WEB_SERVER "{foaea web server name}" /M

    SETX FILEBROKER_DB_SERVER "{file broker database server name}" /M
    SETX FILEBROKER_API_SERVER "{file broker api server name}" /M
    SETX FILEBROKER_WEB_SERVER "{file broker web server name}" /M

    SETX PRODUCTION_WEB_SERVER "{prod web server name}" /M 
    SETX PRODUCTION_OPS_SERVER "{prod ops server name}" /M

    SETX FILEBROKER_TOKEN_KEY "{token key}" /M
    SETX FILEBROKER_TOKEN_ISSUER "{issuer}" /M
    SETX FILEBROKER_TOKEN_AUDIENCE "{audience}}" /M

    SETX FILEBROKER_FOAEA_USERNAME "{FOAEA user name}" /M
    SETX FILEBROKER_FOAEA_USERPASSWORD "{FOAEA user password}" /M
    SETX FILEBROKER_FOAEA_SUBMITTER "{FOAEA submitter}" /M

    SETX FILEBROKER_API_USERNAME "{File Broker user name}" /M
    SETX FILEBROKER_API_USERPASSWORD "{File Broker user password}" /M

Note that the web server name can be localhost if running locally.

