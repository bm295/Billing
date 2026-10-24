# Collection queue position preview

Billing operators can preview where an invoice would move in a proposed collection
queue after reversing suffixes of that queue. This is a planning API: the caller
supplies a queue snapshot's size and the invoice's original position. It does not
load invoices, create a persisted queue, schedule payments, or execute collections.
Use the result only for the supplied snapshot; concurrent queue changes require a
new preview.

`POST /analytics/collection-queue/position-preview`

```json
{
  "queueSize": 5,
  "originalPosition": 1,
  "reversalStarts": [0, 1, 2, 3, 4]
}
```

Response: `{"finalPosition":3}`.

All positions are zero-based. Each start reverses the inclusive range
`[start, queueSize - 1]` in the current queue, in request order. For example,
the sequence above changes `[A,B,C,D,E]` to `[E,A,D,B,C]`, moving B from 1 to 3.
Arbitrary starts, repeated starts, and an empty list are supported. Supplying
`[0,1,...,queueSize-1]` reproduces the original Java algorithm.

The queue must be nonempty and all positions must be inside it. Missing fields,
null starts, and out-of-range values return HTTP 400 with code `invalid_request`.

Only the tracked position is updated: when `position >= start`, its next value is
`queueSize - 1 - (position - start)`. This costs O(number of reversals) time and
O(1) auxiliary space, excluding the request list, regardless of queue size.
The unit tests compare the result with actual list reversals, including the
original sequential reversal pattern; API tests cover JSON and validation.
