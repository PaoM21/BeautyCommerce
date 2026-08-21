import { describe, expect, it } from "vitest";

import { estimateShippingCost, getValidNextStatuses } from "./orderService";

describe("estimateShippingCost", () => {
  it("returns the Bogotá cost for Bogotá D.C.", () => {
    expect(estimateShippingCost("Bogotá D.C.")).toBe(8000);
  });

  it("ignores case and accents when matching Bogotá", () => {
    expect(estimateShippingCost("bogota")).toBe(8000);
    expect(estimateShippingCost("BOGOTA")).toBe(8000);
  });

  it("ignores leading/trailing whitespace", () => {
    expect(estimateShippingCost("  Bogotá  ")).toBe(8000);
  });

  it("returns the national cost for any other city", () => {
    expect(estimateShippingCost("Medellín")).toBe(15000);
    expect(estimateShippingCost("Cali")).toBe(15000);
  });
});

describe("getValidNextStatuses", () => {
  it("mirrors the backend OrderStatusValidator transitions", () => {
    expect(getValidNextStatuses("Pending")).toEqual(["Paid", "Cancelled"]);
    expect(getValidNextStatuses("Paid")).toEqual(["Processing", "Cancelled"]);
    expect(getValidNextStatuses("Processing")).toEqual(["Shipped"]);
    expect(getValidNextStatuses("Shipped")).toEqual(["Delivered"]);
  });

  it("returns no valid transitions for terminal statuses", () => {
    expect(getValidNextStatuses("Delivered")).toEqual([]);
    expect(getValidNextStatuses("Cancelled")).toEqual([]);
  });

  it("returns an empty array for an unknown status", () => {
    expect(getValidNextStatuses("NotAStatus")).toEqual([]);
  });
});
