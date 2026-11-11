import { describe, it, expect, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import React from 'react';
import { StudentProvider } from '../../context/StudentContext';
import { ClassRow } from '../ClassRow';
import { ClassItem, StudentHold } from '../../types';

describe('ClassRow Dynamic Button Transitions', () => {
  const dummyClass: ClassItem = {
    id: 'c1',
    name: 'Grade 1 - Section A',
    totalSeats: 30,
    heldSeats: 0,
    confirmedSeats: 0,
    availableSeats: 30,
  };

  const renderWithContext = (ui: React.ReactElement) => {
    return render(<StudentProvider>{ui}</StudentProvider>);
  };

  it('renders [ Apply ] button when available seats > 0 and no active hold', () => {
    renderWithContext(
      <table>
        <tbody>
          <ClassRow classItem={dummyClass} onHoldUpdated={vi.fn()} />
        </tbody>
      </table>
    );

    expect(screen.getByRole('button', { name: /Apply for Grade 1 - Section A/i })).toBeInTheDocument();
  });

  it('renders [ Class Full ] when available seats == 0 and no active hold', () => {
    const fullClass: ClassItem = { ...dummyClass, availableSeats: 0 };
    renderWithContext(
      <table>
        <tbody>
          <ClassRow classItem={fullClass} onHoldUpdated={vi.fn()} />
        </tbody>
      </table>
    );

    const button = screen.getByRole('button', { name: /Class Full/i });
    expect(button).toBeInTheDocument();
    expect(button).toBeDisabled();
  });

  it('renders [ HELD (MM:SS) - Pay Fee ] and [ Cancel ] when hold is active', () => {
    const futureDate = new Date(Date.now() + 300 * 1000).toISOString(); // 5 minutes
    const activeHold: StudentHold = {
      holdId: 'h1',
      classId: 'c1',
      className: 'Grade 1 - Section A',
      status: 'HELD',
      expiresAt: futureDate,
    };

    renderWithContext(
      <table>
        <tbody>
          <ClassRow classItem={dummyClass} hold={activeHold} onHoldUpdated={vi.fn()} />
        </tbody>
      </table>
    );

    expect(screen.getByRole('button', { name: /Pay Fee for Hold in Grade 1 - Section A/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Cancel Hold/i })).toBeInTheDocument();
  });

  it('renders [ CONFIRMED ] when hold status is CONFIRMED', () => {
    const confirmedHold: StudentHold = {
      holdId: 'h1',
      classId: 'c1',
      className: 'Grade 1 - Section A',
      status: 'CONFIRMED',
      expiresAt: new Date().toISOString(),
    };

    renderWithContext(
      <table>
        <tbody>
          <ClassRow classItem={dummyClass} hold={confirmedHold} onHoldUpdated={vi.fn()} />
        </tbody>
      </table>
    );

    const button = screen.getByRole('button', { name: /Confirmed Admission/i });
    expect(button).toBeInTheDocument();
    expect(button).toBeDisabled();
  });

  it('transitions to [ EXPIRED ] when countdown timer expires without page reload', () => {
    vi.useFakeTimers();

    const nearExpiryDate = new Date(Date.now() + 2 * 1000).toISOString(); // 2 seconds
    const expiringHold: StudentHold = {
      holdId: 'h1',
      classId: 'c1',
      className: 'Grade 1 - Section A',
      status: 'HELD',
      expiresAt: nearExpiryDate,
    };

    const onHoldUpdatedMock = vi.fn();

    renderWithContext(
      <table>
        <tbody>
          <ClassRow classItem={dummyClass} hold={expiringHold} onHoldUpdated={onHoldUpdatedMock} />
        </tbody>
      </table>
    );

    expect(screen.getByRole('button', { name: /Pay Fee for Hold/i })).toBeInTheDocument();

    // Fast-forward 3 seconds
    act(() => {
      vi.advanceTimersByTime(3000);
    });

    expect(screen.getByRole('button', { name: /Hold Expired/i })).toBeInTheDocument();
    expect(onHoldUpdatedMock).toHaveBeenCalled();

    vi.useRealTimers();
  });
});
