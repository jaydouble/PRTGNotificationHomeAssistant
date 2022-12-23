package main

import (
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"runtime"
	"strconv"
	"strings"
	"syscall"

	"github.com/alexflint/go-arg"
)

type opts struct {
	Verbose  bool   `arg:"-v,--verbose,env:PN_VERBOSE" default:"false" help:"Verbosity level"`
	HTTPPort int    `arg:"-p,--httpport,env:PN_PORT" default:"80" help:"Port number"`
	Path     string `arg:"-q,--path,env:PN_PATH" default:"/" help:"path to listen to eg. '/QuasiSecr3tP4th'"`
	Host     string `arg:"-h,--host,env:PN_HAHOST" default:"http://homeassistant:8123" help:"url for Home Assistant`
	Token    string `arg:"-t,--token,env:"PN_HA_TOKEN" default:"" help:"accesstoken to access Home Assistant`
}

var (
	opt opts
)

func l(format string, args ...interface{}) {
	var functionname = ""
	if opt.Verbose {
		pc, _, _, ok := runtime.Caller(1)
		details := runtime.FuncForPC(pc)
		if ok && details != nil {
			functionname = strings.Replace(details.Name(), "main.", "", 1)
		}
		format = "[%-6.6s] " + format
		args = append([]interface{}{functionname}, args...)
		a(format, args...)
	}
}

func a(format string, args ...interface{}) {
	log.Printf(format, args...)
}

func (opts) Description() string {
	return fmt.Sprintf("This is DNS server that is serving on http://0.0.0.0:%d%s", opt.HTTPPort, opt.Path)
}

func main() {
	arg.MustParse(&opt)
	a("start")
	// setup server
	srv := &server{path: opt.Path, host: opt.Host, token: opt.Token}
	go func() {
		a("[Server] HTTP started, and listing to: http://0.0.0.0:%d%s", opt.HTTPPort, opt.Path)
		http.HandleFunc("/", srv.defaultHandler)
		if err := http.ListenAndServe(":"+strconv.Itoa(opt.HTTPPort), nil); err != nil {
			log.Fatalf("Failed to start web server: %s\n", err)
		}
	}()

	sig := make(chan os.Signal, 1)
	signal.Notify(sig, syscall.SIGINT, syscall.SIGTERM)
	s := <-sig
	log.Fatalf("[main] Signal (%v) received, stopping\n", s)
}
