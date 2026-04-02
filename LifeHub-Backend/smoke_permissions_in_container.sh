#!/bin/sh
set -e

base="http://localhost:8080"

extract_token() {
  printf "%s" "$1" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p'
}

request_status() {
  method="$1"
  url="$2"
  token="$3"
  body="$4"

  if [ -n "$token" ]; then
    auth_header="Authorization: Bearer $token"
    if [ -n "$body" ]; then
      curl -sS -o /tmp/resp_body.txt -w "%{http_code}" -X "$method" "$url" -H "$auth_header" -H "Content-Type: application/json" -d "$body"
    else
      curl -sS -o /tmp/resp_body.txt -w "%{http_code}" -X "$method" "$url" -H "$auth_header"
    fi
  else
    if [ -n "$body" ]; then
      curl -sS -o /tmp/resp_body.txt -w "%{http_code}" -X "$method" "$url" -H "Content-Type: application/json" -d "$body"
    else
      curl -sS -o /tmp/resp_body.txt -w "%{http_code}" -X "$method" "$url"
    fi
  fi
}

# Login admin
admin_login=$(curl -sS -X POST "$base/api/auth/login" -H "Content-Type: application/json" -d '{"email":"admin@lifehub.com","password":"Admin123!"}')
admin_token=$(extract_token "$admin_login")
if [ -z "$admin_token" ]; then
  echo "FAIL: admin login"
  echo "$admin_login"
  exit 1
fi

# Login viewer/editor user (juan)
juan_login=$(curl -sS -X POST "$base/api/auth/login" -H "Content-Type: application/json" -d '{"email":"juan@lifehub.com","password":"Test123!"}')
juan_token=$(extract_token "$juan_login")
if [ -z "$juan_token" ]; then
  echo "FAIL: juan login"
  echo "$juan_login"
  exit 1
fi

juan_id=$(printf "%s" "$juan_login" | sed -n 's/.*"user":{[^}]*"id":"\([^"]*\)".*/\1/p')
if [ -z "$juan_id" ]; then
  echo "FAIL: could not extract juan id"
  echo "$juan_login"
  exit 1
fi

stamp=$(date +%s)

# Admin creates space
space_payload=$(printf '{"name":"Perm Space %s","description":"perm smoke","privacy":0,"isPublicProfileVisible":false}' "$stamp")
space_status=$(request_status "POST" "$base/api/creativespaces" "$admin_token" "$space_payload")
space_body=$(cat /tmp/resp_body.txt)
if [ "$space_status" != "201" ]; then
  echo "FAIL: create space status=$space_status"
  echo "$space_body"
  exit 1
fi
space_id=$(printf "%s" "$space_body" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')

# Admin creates document in space
doc_payload=$(printf '{"title":"Perm Doc %s","description":"perm","content":"base-content","type":0,"creativeSpaceId":%s}' "$stamp" "$space_id")
doc_status=$(request_status "POST" "$base/api/documents" "$admin_token" "$doc_payload")
doc_body=$(cat /tmp/resp_body.txt)
if [ "$doc_status" != "201" ]; then
  echo "FAIL: create document status=$doc_status"
  echo "$doc_body"
  exit 1
fi
doc_id=$(printf "%s" "$doc_body" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')

# Owner creates first snapshot
snap1_status=$(request_status "POST" "$base/api/documentversions/document/$doc_id/snapshot" "$admin_token" '{"note":"owner-v1"}')
if [ "$snap1_status" != "201" ]; then
  echo "FAIL: owner snapshot status=$snap1_status"
  cat /tmp/resp_body.txt
  exit 1
fi

# Share as Viewer
viewer_payload=$(printf '{"userId":"%s","permissionLevel":0}' "$juan_id")
share_view_status=$(request_status "POST" "$base/api/creativespaces/$space_id/permissions" "$admin_token" "$viewer_payload")
if [ "$share_view_status" != "200" ]; then
  echo "FAIL: share viewer status=$share_view_status"
  cat /tmp/resp_body.txt
  exit 1
fi

# Viewer can list versions
viewer_list_status=$(request_status "GET" "$base/api/documentversions/document/$doc_id" "$juan_token" "")
if [ "$viewer_list_status" != "200" ]; then
  echo "FAIL: viewer list versions status=$viewer_list_status"
  cat /tmp/resp_body.txt
  exit 1
fi

# Viewer cannot create snapshot
viewer_snap_status=$(request_status "POST" "$base/api/documentversions/document/$doc_id/snapshot" "$juan_token" '{"note":"viewer-try"}')
if [ "$viewer_snap_status" != "403" ]; then
  echo "FAIL: viewer snapshot expected 403 got $viewer_snap_status"
  cat /tmp/resp_body.txt
  exit 1
fi

# Upgrade to Editor
editor_payload=$(printf '{"userId":"%s","permissionLevel":1}' "$juan_id")
share_edit_status=$(request_status "POST" "$base/api/creativespaces/$space_id/permissions" "$admin_token" "$editor_payload")
if [ "$share_edit_status" != "200" ]; then
  echo "FAIL: share editor status=$share_edit_status"
  cat /tmp/resp_body.txt
  exit 1
fi

# Editor can create snapshot
editor_snap_status=$(request_status "POST" "$base/api/documentversions/document/$doc_id/snapshot" "$juan_token" '{"note":"editor-ok"}')
if [ "$editor_snap_status" != "201" ]; then
  echo "FAIL: editor snapshot expected 201 got $editor_snap_status"
  cat /tmp/resp_body.txt
  exit 1
fi

echo "PERMISSIONS_SMOKE_OK"
echo "space_id=$space_id"
echo "doc_id=$doc_id"
echo "viewer_list_status=$viewer_list_status"
echo "viewer_snapshot_status=$viewer_snap_status"
echo "editor_snapshot_status=$editor_snap_status"
