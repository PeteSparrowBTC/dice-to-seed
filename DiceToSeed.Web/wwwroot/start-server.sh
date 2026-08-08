#!/bin/sh
# Serves this folder on loopback only, for an offline Tails session.
#
# Why a web server is needed at all: Blazor WebAssembly does not load over file://. The
# browser needs real HTTP for the wasm MIME type, for module loading and for streaming
# compilation. There is no double-click route, and this is not a bug to be worked around.
#
# Why --bind 127.0.0.1 matters: without it, python listens on every interface, and the
# machine you are typing a seed into starts answering the network. With it, nothing outside
# this computer can reach the port. Confirm with:
#
#     ss -tlnp | grep 9876      # expect 127.0.0.1:9876 and nothing else
#
# Then open http://127.0.0.1:9876 in LibreWolf. Not Tor Browser: on Tails it sends 127.0.0.1
# through the Tor proxy and the connection is refused. That can be fixed by adding
# "127.0.0.1, localhost" under "No Proxy for" in about:preferences, but the setting does not
# always survive, so carrying the LibreWolf AppImage on the stick is the shorter path.
#
# Stop the server with Ctrl+C when you have written the words down. Shut Tails down after.

PORT=9876

cd "$(dirname "$0")" || exit 1

echo "Serving $(pwd)"
echo "Open http://127.0.0.1:$PORT in LibreWolf. Ctrl+C to stop."
echo

exec python3 -m http.server "$PORT" --bind 127.0.0.1
