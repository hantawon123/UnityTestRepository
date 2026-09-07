import copy
import time
import unittest
from collector import Store, validate, BOUNDS
from prometheus_client import CollectorRegistry, generate_latest


def batch():
    b = dict(session='a'*32, build='test', role='host', phase='Searching', players=6,
             seq=0, sent_at=time.time(), fusion_available=False, managed_bytes=100,
             rx_bytes=0, tx_bytes=0, resim_ticks=0, forward_ticks=0, gc_collections=1)
    for name in ['frame', 'fusion', 'rtt']:
        b[name+'_buckets'] = [0]*(len(BOUNDS)+1)
        b[name+'_sum'] = 0
    b['frame_buckets'][0] = 2
    b['frame_sum'] = 2
    return b


class CollectorTest(unittest.TestCase):
    def test_contract_dedup_and_expiry(self):
        b = validate(batch())
        s = Store()
        self.assertEqual(s.ingest(b), 202)
        self.assertEqual(s.ingest(b), 202)
        registry = CollectorRegistry()
        registry.register(s)
        text = generate_latest(registry).decode()
        self.assertIn('game_frame_milliseconds_count{build="test",phase="Searching",players="6",role="host"} 2.0', text)
        self.assertNotIn('game_rtt_milliseconds_count{', text)
        b['seq'] = 1
        self.assertEqual(s.ingest(b), 429)
        s.sessions['a'*32] = (1, time.monotonic()-21, ('test','host','6','Searching'), 100, False)
        self.assertIn('game_reporting_peers{build="test",phase="Searching",players="6",role="host"} 0.0', generate_latest(registry).decode())

    def test_invalid_inputs_and_capacity(self):
        for key, value in [('players', 7), ('frame_sum', float('nan')), ('sent_at', 0), ('build','bad"label'), ('seq', True)]:
            b = batch(); b[key] = value
            with self.assertRaises((ValueError, TypeError)):
                validate(b)
        b = batch(); b['frame_buckets'][0] = -1
        with self.assertRaises(ValueError): validate(b)
        s = Store()
        s.sessions = {str(i): (0,time.monotonic(),(),0,False) for i in range(256)}
        self.assertEqual(s.ingest(batch()), 503)


if __name__ == '__main__':
    unittest.main()
