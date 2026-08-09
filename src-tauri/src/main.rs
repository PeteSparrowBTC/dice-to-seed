// The entire desktop shell.
//
// Tauri serves the frontend through its own protocol, in process, so unlike the python-server
// AppImage there is no port bound and nothing listening on any interface. Unlike the Photino
// build there is no dormant dependency: Tauri is actively maintained and uses webkit2gtk-4.1,
// which Tails ships.
//
// This file deliberately does nothing else. No commands are exposed to the frontend, so the
// web layer cannot reach the filesystem, the network or the shell through it.

#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    tauri::Builder::default()
        .run(tauri::generate_context!())
        .expect("dice to seed: failed to start the window");
}
