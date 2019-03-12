# PRTGNotificationHomeAssistant
PRTG notification to Home Assistant

With this tool, you can get a alarm notification from PRTG.

With this command you can setup a notification for PRTG

```
PRTGNotificationHomeAssistant.exe --key "long generated key" -u "http://homeassisanturl" -l "light.noc" -c %colorofstate -s %sensorid
```

Setup PRTG
- First create a notification Template
	- Sign in to PRTG 
	- Goto setup -> account settings -> Notification Templates
	- Create a new template with the following settings:
		- Basic Settings
			- Template Name (something meaningfull for you)
			- Status: Started
		- Notification Summarization
			- Method: Always notify ASAP, never summarize
		- Enable Execute Program
			- Program File: Select PRTGNotificationHomeAssistant.bat
			- Parameters: --key "KEY" -u "HASS URL" -l "LIGHT" -c "%colorofstate" -s %sensorid
				- the KEY is set in Home Assistant
				- the HASS URL is the plain URL of you Home Assistant
				- the LIGHT is the entity_id of you light (or Hue group) that should react on the notification
				- %colorofstate is a PRTG variable (please don't change)
				- %sensorid is a PRTG variable (please don't change)
	- now click Save
	- then you will have something like: ![Settings](https://raw.githubusercontent.com/jaydouble/PRTGNotificationHomeAssistant/master/doc/notification%20settings.png "PRTG Notification Settings")
- then set the notification trigger
	- Sign in to PRTG
	- Goto Devices -> Notification Triggers
		- Creating a trigger use the settings:
			- When sensor state is Down for at least 0 seconds, perform (You beautifull naming of the template you created)
			- When sensor state is Down for at least 300 seconds, perform (You beautifull naming of the template you created) and repeat every 0 minutes 
			- When sensor leaves Down state after a notification was triggered, perform (You beautifull naming of the template you created)
		- repeat this for 
			- Warning
			- Unusual
			- Unknown
			- Up
	- and you will have something like: ![Triggers](https://raw.githubusercontent.com/jaydouble/PRTGNotificationHomeAssistant/master/doc/notification%20triggers.png "PRTG Notification Truggers")
