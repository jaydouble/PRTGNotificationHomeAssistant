# PRTG config in Home Assistant

This new approach of getting notified by blinking lights when some alarm happend in PRTG is not using the notification in PRTG anymore, but using Home Assistant to poll stats from PRTG and then use that to blinking lights.

To make this happen:

#### copy files:
Copy ```prtg.yaml``` into your packages directory

If you don't have a package folder, you can create one, and then add to configuration.yaml:
```yaml
homeassistant:
  packages: !include_dir_named packages
```

Then you need to add some secrets:
```yaml
prtg-api-token: long-random-token
prtg-url: {full prtg url}/api/getstatus.htm
noclight: light.name
```
by example:
```yaml
prtg-api-token: long-random-token
prtg-url: https://prtg.cloudblabla.tld/api/getstatus.htm
noclight: light.noc
```

To create a token, you can follow: [Paessler tutorial](https://www.paessler.com/manuals/prtg/api_keys)

Save all config, restart your home assistant, and it is working. Hopefully...

If you like, you can add to your dashboard to config automotions:
```yaml
type: horizontal-stack
cards:
  - type: entities
    entities:
      - entity: input_datetime.lighton
      - entity: automation.lightson
    show_header_toggle: false
    title: Lichten aan
  - type: entities
    entities:
      - entity: input_datetime.lightoff
      - entity: automation.lightsoff
    show_header_toggle: false
    title: Lichten uit
```
and to see all statusses
```yaml
type: entities
entities:
  - entity: sensor.prtgsensors
  - entity: sensor.prtg_upsens
  - entity: sensor.prtg_alarms
  - entity: sensor.prtg_warnsens
  - entity: sensor.prtg_pausedsens
  - entity: sensor.prtg_unknownsens
  - entity: sensor.prtg_script
title: PRTG
state_color: true
```