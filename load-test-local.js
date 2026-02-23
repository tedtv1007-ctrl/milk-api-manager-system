import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 10,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'], // 失敗率必須小於 1%
    http_req_duration: ['p(95)<500'], // 95% 的請求必須在 500ms 內完成
  },
};

export default function () {
  // 測試後端的健康檢查端點
  const res = http.get('http://host.docker.internal:5001/api/AuditLogs/stats');
  
  check(res, {
    'status is 200': (r) => r.status === 200,
  });

  sleep(1);
}
