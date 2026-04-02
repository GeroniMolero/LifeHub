#!/bin/sh
set -e

base="http://localhost:8080"

login_resp=$(curl -sS -X POST "$base/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@lifehub.com","password":"Admin123!"}')

token=$(printf "%s" "$login_resp" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [ -z "$token" ]; then
  echo "LOGIN_FAIL"
  echo "$login_resp"
  exit 1
fi

stamp=$(date +%s)

space_resp=$(curl -sS -X POST "$base/api/creativespaces" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Smoke Space $stamp\",\"description\":\"smoke test\",\"privacy\":0,\"isPublicProfileVisible\":false}")

space_id=$(printf "%s" "$space_resp" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')
if [ -z "$space_id" ]; then
  echo "SPACE_FAIL"
  echo "$space_resp"
  exit 1
fi

doc_resp=$(curl -sS -X POST "$base/api/documents" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Smoke Doc $stamp\",\"description\":\"smoke\",\"content\":\"original-content\",\"type\":0,\"creativeSpaceId\":$space_id}")

doc_id=$(printf "%s" "$doc_resp" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')
if [ -z "$doc_id" ]; then
  echo "DOC_FAIL"
  echo "$doc_resp"
  exit 1
fi

snapshot_resp=$(curl -sS -X POST "$base/api/documentversions/document/$doc_id/snapshot" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d '{"note":"smoke-v1"}')

version_id=$(printf "%s" "$snapshot_resp" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')
if [ -z "$version_id" ]; then
  echo "SNAPSHOT_FAIL"
  echo "$snapshot_resp"
  exit 1
fi

update_resp=$(curl -sS -X PUT "$base/api/documents/$doc_id" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Smoke Doc Updated $stamp\",\"description\":\"smoke\",\"content\":\"updated-content\"}")

restore_resp=$(curl -sS -X POST "$base/api/documentversions/$version_id/restore" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d '{}')

final_doc=$(curl -sS -X GET "$base/api/documents/$doc_id" \
  -H "Authorization: Bearer $token")

echo "SMOKE_OK"
echo "space_id=$space_id"
echo "doc_id=$doc_id"
echo "version_id=$version_id"
echo "restore_resp=$restore_resp"
echo "final_doc=$final_doc"