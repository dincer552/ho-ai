#!/usr/bin/env bash
set -Eeuo pipefail

# One-time setup helper for the Azure VM.
# Run on the VM after creating a GitHub Actions runner registration token
# for repository dincer552/ho-ai.
#
# Required environment variables:
#   RUNNER_URL   e.g. https://github.com/dincer552/ho-ai
#   RUNNER_TOKEN GitHub Actions runner registration token
# Optional:
#   RUNNER_DIR  default: /home/azureuser/actions-runner
#
# This script does NOT build the V5 image. The VM runner is intended only
# to pull the already-built GHCR image and restart the container.

: "${RUNNER_URL:?RUNNER_URL is required}"
: "${RUNNER_TOKEN:?RUNNER_TOKEN is required}"

RUNNER_DIR="${RUNNER_DIR:-/home/azureuser/actions-runner}"
RUNNER_LABELS="${RUNNER_LABELS:-v5,azure-vm}"
RUNNER_ARCH="${RUNNER_ARCH:-linux-x64}"

if ! command -v docker >/dev/null 2>&1; then
  echo "❌ Docker bulunamadı. Önce Docker kurulmalı."
  exit 1
fi

if ! systemctl is-enabled docker >/dev/null 2>&1; then
  echo "⚠️ Docker servisinin boot'ta otomatik başlaması etkin değil."
fi

mkdir -p "${RUNNER_DIR}"
cd "${RUNNER_DIR}"

if [ -f .runner ]; then
  echo "ℹ️ GitHub Actions runner zaten yapılandırılmış: ${RUNNER_DIR}"
  exit 0
fi

RUNNER_VERSION="${RUNNER_VERSION:-2.328.0}"
ARCHIVE="actions-runner-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz"
URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${ARCHIVE}"

if [ ! -f "${ARCHIVE}" ]; then
  echo "📥 GitHub Actions runner indiriliyor: v${RUNNER_VERSION}"
  curl -fL --retry 5 --connect-timeout 20 -o "${ARCHIVE}" "${URL}"
fi

tar xzf "${ARCHIVE}"

./config.sh \
  --url "${RUNNER_URL}" \
  --token "${RUNNER_TOKEN}" \
  --name "v5-azure-vm" \
  --labels "${RUNNER_LABELS}" \
  --work "_work" \
  --unattended

sudo ./svc.sh install "$(whoami)"
sudo ./svc.sh start

sudo ./svc.sh status

echo "✅ V5 self-hosted runner kuruldu ve servis olarak başlatıldı."
echo "📌 Labels: ${RUNNER_LABELS}"
echo "📌 Runner directory: ${RUNNER_DIR}"
echo "📌 VM build/test yapmayacak; sonraki aşamada yalnızca GHCR pull + container deploy çalıştırılacak."
