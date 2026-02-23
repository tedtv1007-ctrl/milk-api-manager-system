import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 20, // 增加到 20 個並發
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<100'], // 網關轉發應更迅速
  },
};

export default function () {
  // 透過 APISIX 網關訪問 (Port 9080)
  // 我們假設 /api/v1/pii-test 是一個會觸發 PII 遮蔽的路由
  const res = http.get('http://host.docker.internal:9080/api/AuditLogs/stats');
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    // 如果插件生效，ResponseBody 中不應出現明文的敏感關鍵字
    // 'pii is masked': (r) => r.body.includes('***'), 
  });

  sleep(0.5); // 提高頻率
}
