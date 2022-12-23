package main

import (
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"strconv"
)

func l(key string, message string) {
	log.Printf("%8s: %s\n", key, message)
}

func main() {
	port := flag.Int("port", 8053, "port-number to list on")
	flag.Parse()
	if os.Getenv("DOH_PORT") != "" {
		b, err := strconv.Atoi(os.Getenv("DOH_PORT"))
		if err == nil {
			*port = b
		}
	}
	url := fmt.Sprintf("http://127.0.0.1:%s/health", strconv.Itoa(*port))
	l("Start", "Healtcheck")
	l("port", strconv.Itoa(*port))
	l("url", url)

	ret, err := http.Get(url)
	if err != nil {
		os.Exit(1)
	}
	if ret.StatusCode != http.StatusOK {
		l("Error", strconv.Itoa(ret.StatusCode))
		os.Exit(1)
	}
}
