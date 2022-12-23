FROM golang as builder

RUN mkdir -p /app/{src,dist}

COPY src /app/src

RUN cd /app/src/prtgnotify/ && \
    CGO_ENABLED=0 GOOS=linux go build -o /app/dist/prtgnotify . && \
    cd /app/src/healthcheck/ && \
    CGO_ENABLED=0 GOOS=linux go build -o /app/dist/healthcheck .


FROM scratch
COPY --from=builder /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=builder /app/dist/prtgnotify /app/dist/healthcheck /

HEALTHCHECK --interval=15s --timeout=1s --start-period=60s --retries=3 CMD [ "/healthcheck" ]

ENTRYPOINT [ "/prtgnotify" ]
EXPOSE 80