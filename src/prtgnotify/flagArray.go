/*
*
flag array
*/
package main

import "strings"

type flagArray []string

func (i *flagArray) String() string {
	// change this, this is just can example to satisfy the interface
	return strings.Join(*i, ", ")
}

func (i *flagArray) UnmarshalText(value []byte) error {
	parts := strings.Split(string(value), "|")
	for _, a := range parts {
		*i = append(*i, strings.TrimSpace(a))
	}
	return nil
}
