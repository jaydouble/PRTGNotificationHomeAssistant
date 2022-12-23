/*
*
server package
*/
package main

import (
	"encoding/json"
	"net/http"
	"net/url"
	"path"
	"runtime"
	"strconv"
	"strings"
)

type server struct {
	path  string
	host  string
	token string
	light string
}

func (s *server) httpError(w http.ResponseWriter, httpErrorCode int, args ...interface{}) {
	_, filename, line, _ := runtime.Caller(1)
	args = append([]interface{}{httpErrorCode}, args...)       // third element in args
	args = append([]interface{}{line}, args...)                // second element in args
	args = append([]interface{}{path.Base(filename)}, args...) //first element in args
	a("[httpError] %s:%d httpError %d '%s', ip: %15s, url: %s", args...)
	http.Error(w, http.StatusText(httpErrorCode), httpErrorCode)
}

func (s *server) getIP(r *http.Request) string {
	ipAddress := r.RemoteAddr
	fwdAddress := r.Header.Get("X-Forwarded-For") // capitalisation doesn't matter
	if fwdAddress != "" {
		// Got X-Forwarded-For
		ipAddress = fwdAddress // If it's a single IP, then awesome!

		// If we got an array... grab the first IP
		ips := strings.Split(fwdAddress, ", ")
		if len(ips) > 1 {
			ipAddress = ips[0]
		}
	} else {
		// ipAddress is now Ip with port number, so we need to
		// remove the port number
		p := strings.LastIndex(ipAddress, ":")
		ipAddress = ipAddress[:p]
	}
	return ipAddress
}

func Hex2RGB(hex string) ([]uint8, error) {
	var rgb []uint8
	values, err := strconv.ParseUint(string(hex), 16, 32)

	if err != nil {
		return []uint8{}, err
	}

	rgb = []uint8{uint8(values >> 16), uint8((values >> 8) & 0xFF), uint8(values & 0xFF)}

	return rgb, nil
}

func (s *server) setLight(ho_host string, accesstoken string, lightname string, color string) bool {
	type hass_command struct {
		Rgb_color  []uint8 `json:"rgb_color"`
		Brightness int     `json:"brightness"`
		Entity_id  string  `json:"entity_id"`
		Flash      string  `json:"flash"`
	}
	color = strings.Replace(color, "#", "", 1)
	rgb, err := Hex2RGB(color)
	if err != nil {
		a("Failed to parse hex to RGB, %s", err.Error())
		return false
	}

	command := hass_command{
		Rgb_color:  rgb,
		Brightness: 2,
		Entity_id:  lightname,
		Flash:      "",
	}
	switch color {
	case "#b4cc38":

	}
	b, err := json.Marshal(command)
	if err != nil {
		a("failed to create json: %s", err.Error())
		return false
	}
	a("Command: %+v", string(b))
	return true
}

func (s *server) replacePrivacyToken(inputURL string) string {
	urlA, err := url.Parse(inputURL)
	if err != nil {
		a(err.Error())
	}

	// Use the Query() method to get the query string params as a url.Values map.
	values := urlA.Query()

	// Make the changes that you want using the Add(), Set() and Del() methods. If
	// you want to retrieve or check for a specific parameter you can use the Get()
	// and Has() methods respectively.
	if values.Has("token") {
		values.Set("token", "-redactedForPrivacy-")
	}

	// Use the Encode() method to transform the url.Values map into a URL-encoded
	// string (like "age=29&name=alice...") and assign it back to the URL. Note
	// that the encoded values will be sorted alphabetically based on the parameter
	// name.
	urlA.RawQuery = values.Encode()

	return urlA.String()
}

func (s *server) defaultHandler(w http.ResponseWriter, r *http.Request) {
	var status int
	status = 200
	ipAddress := s.getIP(r)
	if r.URL.Path == "/health" {
		s.healthCheck(w, r)
	} else if s.path == r.URL.Path {
		vars := r.URL.Query()
		if len(vars["host"]) == 0 {
			vars["host"] = append(vars["host"], s.host)
		}
		if len(vars["token"]) == 0 {
			vars["token"] = append(vars["token"], s.token)
		}
		if len(vars["light"]) == 0 {
			vars["light"] = append(vars["light"], s.light)
		}

		// a("%+v", vars)
		if r.Method != "GET" || vars["host"][0] == "" || vars["token"][0] == "" || vars["light"][0] == "" {
			s.httpError(w, http.StatusBadRequest, "", ipAddress, r.RequestURI)
			status = http.StatusBadRequest
		} else {
			s.setLight(vars["host"][0], vars["token"][0], vars["light"][0], vars["color"][0])
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte("{}"))
		}
	} else {
		s.httpError(w, http.StatusBadRequest, "", ipAddress, r.RequestURI)
		status = http.StatusNotFound
	}
	a("[ACCLOG] %15s, %d, %s", ipAddress, status, s.replacePrivacyToken(r.RequestURI))
}

func (s *server) healthCheck(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "text/html")
	w.Write([]byte("All OK"))
}
