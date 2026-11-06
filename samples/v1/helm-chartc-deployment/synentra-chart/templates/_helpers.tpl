{{- define "synentra.name" -}}
synentra
{{- end }}

{{- define "synentra.fullname" -}}
{{- printf "%s-%s" .Release.Name (include "synentra.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{- define "synentra.labels" -}}
app.kubernetes.io/name: {{ include "synentra.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | quote }}
{{- end }}
