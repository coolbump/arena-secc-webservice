
Arena SECC Web Services
===============================================================================
This project was forked from the Arena HDC Web Service project which was developed as an extension to the built-in Arena Web Services. It provides a new API endpoint through which the internal Arena API can still be accessed. This package also provides extra functionality to extend the Arena Web Services API as well as allows 3rd party DLLs to provide even more functionality by dynamically adding references to new DLLs at run-time.

##Setup
In order to setup this project in Arena, follow these steps:
* Build the project
* Drop the resulting Arena.Custom.HDC.WebService.dll in your Arena bin directory
* Take the api.ashx and drop it in the root of your Arena install
* You should be able to use the following links to see your environment:
 * **Info** - [http://{Your Arena Domain}/api.ashx/rc/info](http://{Your Arena Domain}/api.ashx/rc/info)
 * **Help** - [http://{Your Arena Domain}/api.ashx/help](http://{Your Arena Domain}/api.ashx/help)

##Development
To create a new API, you can modify any of the existing *API.cs classes.  These are in the base
of the project as well as the SECC folder.  The SECC ones are specific customizations for
Southeast Christian Church.  Typically, we encourage extending the API by creating your own
*API.cs classes in your organization's own namespace.  if you do that, you'll need to register
your new classes in the RegisterInternalHandlers() method of RestAPI.cs (Around Line 120).

###[Southeast Christian Church](http://www.southeastchristian.org/)
Southeast Christian Church in Louisville, Kentucky is an evangelical Christian church. In our 
mission to connect people to Jesus and one another, Southeast Christian Church has 
grown into a unified multisite community located in the Greater Louisville/Southern Indiana region. 
We have multiple campuses serving the specific needs of the areas in which they are located, 
while receiving centralized leadership and teaching from the campus on Blankenbaker Parkway.